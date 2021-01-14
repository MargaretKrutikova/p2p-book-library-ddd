namespace Core.Domain

open System
open FsToolkit.ErrorHandling.ResultCE

module Errors =
    type ValidationError =
        | TitleInvalid
        | AuthorInvalid
        | UserNotFound
        | ListingNotFound

    type DomainError =
        | ListingNotEligibleForOperation
        | ListingAlreadyRequestedByUser
        | BorrowErrorListingIsNotAvailable 
        | ListingIsAlreadyBorrowed
        | ListingIsNotRequested
        | ListingIsNotBorrowed

    type AppError =
        | Validation of ValidationError
        | Domain of DomainError
        | ServiceError
        static member toDomain error = Domain error |> Error
        
module Types =
    type UserId = private UserId of Guid
    type ListingId = private ListingId of Guid
    type Title = private Title of string
    type Author = private Author of string

    type ListingStatus =
        | Available
        | RequestedToBorrow of UserId
        | Borrowed of UserId
        
    // TODO: use smart constructor
    type UserName = string
    type User = { UserId: UserId; Name: UserName }

    type BookListing =
        { ListingId: ListingId
          OwnerId: UserId
          Author: Author
          Title: Title
          Status: ListingStatus }

    module UserId =
        let value ((UserId id)) = id
        let create guid = UserId guid

    module ListingId =
        let value ((ListingId id)) = id
        let create guid = ListingId guid

    module Title =
        open Errors

        let create value: Result<Title, ValidationError> =
            if String.IsNullOrWhiteSpace value
               || value.Length > 200 then
                Error TitleInvalid
            else
                value |> Title |> Ok

        let value ((Title str)) = str

    module Author =
        open Errors

        let create value: Result<Author, ValidationError> =
            if String.IsNullOrWhiteSpace value
               || value.Length > 100 then
                Error AuthorInvalid
            else
                value |> Author |> Ok

        let value ((Author str)) = str

module Messages =
    open Types

    type PublishBookListingArgs =
        { NewListingId: ListingId
          UserId: UserId
          Title: string
          Author: string }

    type RegisterUserArgs = { UserId: UserId; Name: string }
    type ChangeListingStatusCommand =
        | RequestToBorrow
        | CancelRequestToBorrow
        | ApproveRequestToBorrow
        | ReturnListing
    type ChangeListingStatusArgs = {
        UserId: UserId
        ListingId: ListingId
        DateTime: DateTime
        Command: ChangeListingStatusCommand
    }
    
    [<RequireQualifiedAccess>]
    type Command =
        | RegisterUser of RegisterUserArgs
        | PublishBookListing of PublishBookListingArgs
        | ChangeListingStatus of ChangeListingStatusArgs

    [<RequireQualifiedAccess>]
    type Event =
        | BookListingPublished of ListingId
        | ListingRequestedToBorrow of ListingId * UserId
        | ListingBorrowRequestApproved of ListingId * UserId

    [<RequireQualifiedAccess>]
    type Query =
        | GetAllPublishedBookListings
        | GetUsersPublishedBookListings of UserId

module Logic =
    open Errors
    open Types

    let publishBookListing (dto: Messages.PublishBookListingArgs): Result<BookListing, AppError> =
        result {
            let! title =
                Title.create dto.Title
                |> Result.mapError Validation

            let! author =
                Author.create dto.Author
                |> Result.mapError Validation

            let bookListing: BookListing =
                { ListingId = dto.NewListingId
                  OwnerId = dto.UserId
                  Author = author
                  Title = title
                  Status = Available }

            return bookListing
        }
    
    type BorrowListingRequest = {
        ListingStatus: ListingStatus
        OwnerId: UserId
        BorrowerId: UserId
    }
    
    let requestBorrowListing (request: BorrowListingRequest): Result<ListingStatus, AppError> =
        if request.OwnerId = request.BorrowerId then
            AppError.toDomain ListingNotEligibleForOperation
        else
            match request.ListingStatus with
            | Available -> RequestedToBorrow request.BorrowerId |> Ok
            | RequestedToBorrow borrowerId when borrowerId = request.BorrowerId ->
                AppError.toDomain ListingAlreadyRequestedByUser
            | RequestedToBorrow _
            | Borrowed _ -> AppError.toDomain BorrowErrorListingIsNotAvailable
    
    type CancelListingRequest = {
        ListingStatus: ListingStatus
        BorrowerId: UserId
    }
    
    let cancelRequestToBorrow (request: CancelListingRequest): Result<ListingStatus, AppError> =
        request.ListingStatus |> Ok
    
    type ApproveBorrowListingRequest = {
        ListingStatus: ListingStatus
        OwnerId: UserId
        ApproverId: UserId
    }
    
    let approveBorrowListingRequest (request: ApproveBorrowListingRequest): Result<ListingStatus, AppError> =
        if request.OwnerId <> request.ApproverId then
             AppError.toDomain ListingNotEligibleForOperation
        else
            match request.ListingStatus with
            | RequestedToBorrow borrowerId -> Borrowed borrowerId |> Ok
            | Borrowed _ -> AppError.toDomain ListingIsAlreadyBorrowed
            | Available -> AppError.toDomain ListingIsNotRequested
            
    type ReturnListingRequest = {
        ListingStatus: ListingStatus
        BorrowerId: UserId
    }
    
    let returnBookListing (request: ReturnListingRequest): Result<ListingStatus, AppError> =
        match request.ListingStatus with
        | Borrowed borrowerId when request.BorrowerId = borrowerId -> Available |> Ok
        | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation
        | RequestedToBorrow _
        | Available -> AppError.toDomain ListingIsNotBorrowed