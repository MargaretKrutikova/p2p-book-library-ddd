module Api.ApiHandlers

open Api.CompositionRoot
open Api.Models
open Core.Domain.Errors
open Core.Domain.Messages
open Core.Domain.Types
open Core.Handlers.QueryHandlers

open Core.QueryModels
open FsToolkit.ErrorHandling
open System

module ModelConversions =
    let toPublishedListingsOutputModel listings: PublishedListingsOutputModel = { Listings = listings }
    let toUserListingsOutputModel listings: UserListingsOutputModel = { Listings = listings }

module CommandArgsConversions =
    let toPublishBookListingArgs (listingId: Guid) (listingModel: ListingPublishInputModel): PublishBookListingArgs =
        { NewListingId = ListingId.create listingId
          UserId = UserId.create listingModel.UserId
          Title = listingModel.Title
          Author = listingModel.Author }

    let toRegisterUserArgs (userId: Guid) (inputModel: UserRegisterInputModel): RegisterUserArgs =
        { UserId = UserId.create userId
          Name = inputModel.Name }

    let toRequestListingArgs (model: RequestBorrowListingInputModel): RequestToBorrowListingArgs =
        { ListingId = model.ListingId |> ListingId.create
          BorrowerId = model.BorrowerId |> UserId.create }

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
            CommandArgsConversions.toRegisterUserArgs userId userModel
            |> Command.RegisterUser

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError

        let response: UserRegisteredOutputModel = { Id = userId }
        return response
    }

let publishListing (root: CompositionRoot) (listingModel: ListingPublishInputModel) =
    taskResult {
        let listingId = Guid.NewGuid()

        let command =
            CommandArgsConversions.toPublishBookListingArgs listingId listingModel
            |> Command.PublishBookListing

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError

        let response: ListingPublishedOutputModel = { Id = listingId }
        return response
    }

let requestBorrowListing (root: CompositionRoot) (inputModel: RequestBorrowListingInputModel) =
    taskResult {
        let command =
            CommandArgsConversions.toRequestListingArgs inputModel
            |> Command.RequestToBorrowBook

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError
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
            { UserId = user.Id 
              Name = user.Name }

        return response
    }

let getUserListings (root: CompositionRoot) (userId: Guid) =
    taskResult {
        let! listings =
            UserId.create userId
            |> root.GetUserBookListings
            |> TaskResult.mapError fromQueryError

        return listings |> Seq.toList |> ModelConversions.toUserListingsOutputModel
    }

let getAllPublishedListings (root: CompositionRoot) () =
    taskResult {
        let! listings =
            root.GetAllPublishedListings()
            |> TaskResult.mapError fromQueryError

        return listings |> Seq.toList |> ModelConversions.toPublishedListingsOutputModel
    }
