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
open FsToolkit.ErrorHandling.Operator.TaskResult

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

    let toApproveBorrowRequestArgs (model: ApproveBorrowRequestInputModel): ApproveBorrowListingArgs =
        { ListingId = model.ListingId |> ListingId.create
          ApproverId = model.ApproverId |> UserId.create }
    
    let toReturnListingArgs (model: ReturnListingInputModel): ReturnListingArgs =
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

let private getExistingListingById (queryHandler: QueryHandler) listingId =
    listingId
    |> ListingId.create
    |> queryHandler.GetListingById
    |> TaskResult.mapError fromQueryError
    |> Task.map (Result.bind (Result.requireSome ApiError.InternalError))
    
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
            CommandArgsConversions.toRequestListingArgs inputModel |> Command.RequestToBorrowListing

        do! root.CommandHandler command |> TaskResult.mapError fromAppError
        return! getExistingListingById root.QueryHandler inputModel.ListingId
    }

let approveBorrowRequest (root: CompositionRoot) (inputModel: ApproveBorrowRequestInputModel) =
    taskResult {
        let command =
            CommandArgsConversions.toApproveBorrowRequestArgs inputModel
            |> Command.ApproveBorrowListingRequest

        do! root.CommandHandler command |> TaskResult.mapError fromAppError
        return! getExistingListingById root.QueryHandler inputModel.ListingId
    }
    
let returnListing (root: CompositionRoot) (inputModel: ReturnListingInputModel) =
    taskResult {
        let command =
            CommandArgsConversions.toReturnListingArgs inputModel
            |> Command.ReturnListing

        do! root.CommandHandler command |> TaskResult.mapError fromAppError
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
