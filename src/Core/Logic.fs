module Core.Logic

open Core.Commands
open Core.Events
open Domain.Errors
open Domain.Types
open FsToolkit.ErrorHandling.ResultCE

let publishBookListing (dto: PublishBookListingArgs): Result<BookListing, AppError> =
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

type ListingReadModel =
    { Id: ListingId
      OwnerId: UserId
      Status: ListingStatus }

let requestToBorrowListing (listing: ListingReadModel) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
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

let cancelRequestToBorrowListing (listing: ListingReadModel) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
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

let approveRequestToBorrowListing (listing: ListingReadModel) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
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

let returnBorrowedListing (listing: ListingReadModel) (args: ChangeListingStatusArgs): Result<Event * ListingStatus, AppError> =
    let borrowerId = args.ChangeRequestedByUserId
    match listing.Status with
    | Borrowed currentBorrowerId when borrowerId = currentBorrowerId ->
        let event = Event.ListingReturned { ListingId = listing.Id; BorrowerId = currentBorrowerId }
        (event, Available) |> Ok
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation
    | RequestedToBorrow _
    | Available -> AppError.toDomain ListingIsNotBorrowed

let changeListingStatus (listing: ListingReadModel) (args: ChangeListingStatusArgs) =
    match args.Command with
    | RequestToBorrow -> requestToBorrowListing listing args
    | CancelRequestToBorrow -> cancelRequestToBorrowListing listing args
    | ApproveRequestToBorrow -> approveRequestToBorrowListing listing args
    | ReturnBorrowedListing -> returnBorrowedListing listing args