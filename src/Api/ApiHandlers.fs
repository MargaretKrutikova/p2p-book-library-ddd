module Api.ApiHandlers

open Api.CompositionRoot
open Api.Models

open Core.Domain.Errors
open Core.Domain.Messages
open Core.Domain.Types
open Core.Handlers

open Core.Handlers.QueryHandlers
open FsToolkit.ErrorHandling
open System

let private toUserListingOutputModel (listing: QueryHandlers.UserBookListingDto): UserListingOutputModel =
    { Id = ListingId.value listing.ListingId
      Author = listing.Author
      Title = listing.Title }

let private toPublishBookListingArgs (listingId: Guid) (listingModel: ListingPublishInputModel): PublishBookListingArgs =
    { NewListingId = ListingId.create listingId
      UserId = UserId.create listingModel.UserId
      Title = listingModel.Title
      Author = listingModel.Author }

let private toRegisterUserArgs (userId: Guid) (inputModel: UserRegisterInputModel): RegisterUserArgs =
    { UserId = UserId.create userId
      Name = inputModel.Name }

let private fromQueryError (queryError: QueryError): ApiError =
    match queryError with
    | InternalError -> ApiError.InternalError

let private fromAppError (appError: AppError): ApiError =
    match appError with
    | Validation error -> ValidationError error
    | Domain error -> DomainError error
    | ServiceError -> ApiError.InternalError

let registerUser (root: CompositionRoot) (userModel: UserRegisterInputModel) =
    taskResult {
        let userId = Guid.NewGuid()

        let command =
            toRegisterUserArgs userId userModel
            |> Command.RegisterUser

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError
            |> TaskResult.ignore

        let response: UserRegisteredOutputModel = { Id = userId }
        return response
    }

let publishListing (root: CompositionRoot) (listingModel: ListingPublishInputModel) =
    taskResult {
        let listingId = Guid.NewGuid()

        let command =
            toPublishBookListingArgs listingId listingModel
            |> Command.PublishBookListing

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError
            |> TaskResult.ignore

        let response: ListingPublishedOutputModel = { Id = listingId }
        return response
    }

let loginUser (root: CompositionRoot) (userModel: UserLoginInputModel) =
    taskResult {
        let! userOption =
            root.GetUserByName userModel.Name
            |> TaskResult.mapError (fun _ -> ApiError.LoginFailure)

        let! user =
            userOption
            |> Result.requireSome ApiError.LoginFailure

        let response: UserOutputModel =
            { UserId = user.Id |> UserId.value
              Name = user.Name }

        return response
    }

let getUserListings (root: CompositionRoot) (userId: Guid) =
    taskResult {
        let! listings =
            UserId.create userId
            |> root.GetUserBookListings
            |> TaskResult.mapError fromQueryError

        return listings
               |> Seq.map toUserListingOutputModel
               |> Seq.toList
    }
