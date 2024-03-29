module Api.ApiHandlers

open Api.CompositionRoot
open Api.Models
open Core.Commands
open Core.Domain.Errors
open Core.Domain.Types

open System
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.TaskResult
open Services.QueryHandlers
open Services.QueryModels

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
          Name = inputModel.Name
          Email = inputModel.Email
          IsSubscribedToUserListingActivity = inputModel.IsSubscribedToUserListingActivity }
    
    let private toChangeListingStatusArgs' (listingId: Guid) (userId: Guid) command: ChangeListingStatusArgs =
        { ChangeRequestedByUserId = userId |> UserId.create; ListingId = ListingId.create listingId; Command = command }

    let toChangeListingStatusArgs (inputModel: ChangeListingStatusInputModel): ChangeListingStatusArgs =
        let command =
            match inputModel.Command with
            | ChangeListingStatusInputCommand.RequestToBorrow ->
                ChangeListingStatusCommand.RequestToBorrow 
            | ChangeListingStatusInputCommand.CancelRequestToBorrow ->
                ChangeListingStatusCommand.CancelRequestToBorrow 
            | ChangeListingStatusInputCommand.ApproveRequestToBorrow ->
                ChangeListingStatusCommand.ApproveRequestToBorrow 
            | ChangeListingStatusInputCommand.ReturnListing ->
                ChangeListingStatusCommand.ReturnBorrowedListing 
        
        toChangeListingStatusArgs' inputModel.ListingId inputModel.UserId command
        
let private fromQueryError (queryError: QueryError): ApiError =
    match queryError with
    | QueryError.InternalError -> ApiError.InternalError

let private fromAppError (appError: AppError): ApiError =
    match appError with
    | Validation error -> ValidationError error
    | Domain error -> DomainError error
    | ServiceError -> ApiError.InternalError

let private getExistingListingById (queryHandler: QueryHandler) listingId =
    listingId
    |> ListingId.create
    |> queryHandler.GetListingById
    |> TaskResult.mapError fromQueryError
    
let registerUser (root: CompositionRoot) (userModel: UserRegisterInputModel) =
    taskResult {
        let userId = Guid.NewGuid()

        let command =
            CommandArgsConversions.toRegisterUserArgs userId userModel
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
            CommandArgsConversions.toPublishBookListingArgs listingId listingModel
            |> Command.PublishBookListing

        do! root.CommandHandler command
            |> TaskResult.mapError fromAppError
            |> TaskResult.ignore

        let response: ListingPublishedOutputModel = { Id = listingId }
        return response
    }
    
let changeListingStatus (root: CompositionRoot) (inputModel: ChangeListingStatusInputModel) =
    taskResult {
        let command =
            CommandArgsConversions.toChangeListingStatusArgs inputModel
            |> Command.ChangeListingStatus

        do! root.CommandHandler command |> TaskResult.mapError fromAppError |> TaskResult.ignore
        return! getExistingListingById root.QueryHandler inputModel.ListingId
    }

let loginUser (root: CompositionRoot) (userModel: UserLoginInputModel) =
    taskResult {
        let! userOption =
            root.QueryHandler.GetUserByName userModel.Name
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
            |> root.QueryHandler.GetUserBookListings
            |> TaskResult.mapError fromQueryError

        return listings |> Seq.toList |> ModelConversions.toUserListingsOutputModel
    }

let getAllPublishedListings (root: CompositionRoot) () =
    taskResult {
        let! listings =
            root.QueryHandler.GetAllPublishedBookListings()
            |> TaskResult.mapError fromQueryError

        return listings |> Seq.toList |> ModelConversions.toPublishedListingsOutputModel
    }

let getUserActivity (root: CompositionRoot) (userId: Guid) =
    userId
    |> UserId.create
    |> root.QueryHandler.GetUserActivity 
    |> TaskResult.mapError fromQueryError
