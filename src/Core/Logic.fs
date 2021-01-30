module Core.Logic

open Core.Commands
open Core.Events
open Domain.Errors
open Domain.Types

open FsToolkit.ErrorHandling.ResultCE
open System

module Validation =
    let assertValidTitle value: Result<unit, ValidationError> =
        if String.IsNullOrWhiteSpace value
           || value.Length > 200 then
            Error TitleInvalid
        else
            Ok ()

    let assertValidAuthor value: Result<unit, ValidationError> =
        if String.IsNullOrWhiteSpace value
           || value.Length > 100 then
            Error AuthorInvalid
        else
            Ok ()

let publishBookListing (dto: PublishBookListingArgs): Result<BookListing, AppError> =
    result {
        do! Validation.assertValidTitle dto.Title |> Result.mapError Validation
        do! Validation.assertValidAuthor dto.Author |> Result.mapError Validation

        let bookListing: BookListing =
            { Id = dto.NewListingId
              OwnerId = dto.UserId
              Author = dto.Author 
              Title = dto.Title
              Status = Available }

        return bookListing
    }

let requestToBorrowListing (listing: BookListing) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
    let requesterId = args.ChangeRequestedByUserId
    if listing.OwnerId = requesterId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match listing.Status with
        | Available ->
            let event = Event.ListingRequestedToBorrow { ListingId = listing.Id; RequesterId = requesterId }
            (event, RequestedToBorrow requesterId) |> Ok
        | RequestedToBorrow borrowerId when borrowerId = requesterId ->
            AppError.toDomain ListingAlreadyRequestedByUser
        | RequestedToBorrow _
        | Borrowed _ -> AppError.toDomain BorrowErrorListingIsNotAvailable

let cancelRequestToBorrowListing (listing: BookListing) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
    let requesterId = args.ChangeRequestedByUserId
    match listing.Status with
    | RequestedToBorrow borrowerId when borrowerId = requesterId ->
        let event = Event.RequestToBorrowCancelled { ListingId = listing.Id; RequesterId = requesterId }
        (event, Available) |> Ok
    | Borrowed borrowerId when borrowerId = requesterId ->
        AppError.toDomain ListingIsAlreadyApproved
    | Available -> AppError.toDomain ListingIsNotRequested
    | RequestedToBorrow _
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation

let approveRequestToBorrowListing (listing: BookListing) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
    let approverId = args.ChangeRequestedByUserId
    if listing.OwnerId <> approverId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match listing.Status with
        | RequestedToBorrow borrowerId ->
            let event = Event.RequestToBorrowApproved { ListingId = listing.Id; BorrowerId = borrowerId }
            (event, Borrowed borrowerId) |> Ok
        | Borrowed _ -> AppError.toDomain ListingIsAlreadyBorrowed
        | Available -> AppError.toDomain ListingIsNotRequested

let returnBorrowedListing (listing: BookListing) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
    let borrowerId = args.ChangeRequestedByUserId
    match listing.Status with
    | Borrowed currentBorrowerId when borrowerId = currentBorrowerId ->
        let event = Event.ListingReturned { ListingId = listing.Id; BorrowerId = currentBorrowerId }
        (event, Available) |> Ok
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation
    | RequestedToBorrow _
    | Available -> AppError.toDomain ListingIsNotBorrowed

let changeListingStatus (listing: BookListing) (args: ChangeListingStatusArgs) =
    match args.Command with
    | RequestToBorrow -> requestToBorrowListing listing args
    | CancelRequestToBorrow -> cancelRequestToBorrowListing listing args
    | ApproveRequestToBorrow -> approveRequestToBorrowListing listing args
    | ReturnBorrowedListing -> returnBorrowedListing listing args