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

type RequestToBorrowListingData =
    { ListingStatus: ListingStatus
      OwnerId: UserId
      RequesterId: UserId }

let requestToBorrowListing (data: RequestToBorrowListingData): Result<DomainEvent, AppError> =
    if data.OwnerId = data.RequesterId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match data.ListingStatus with
        | Available ->
            DomainEvent.ListingRequestedToBorrow { RequesterId = data.RequesterId } |> Ok
        | RequestedToBorrow borrowerId when borrowerId = data.RequesterId ->
            AppError.toDomain ListingAlreadyRequestedByUser
        | RequestedToBorrow _
        | Borrowed _ -> AppError.toDomain BorrowErrorListingIsNotAvailable

type CancelRequestToBorrowListingData =
    { ListingStatus: ListingStatus
      RequesterId: UserId }

let cancelRequestToBorrowListing (data: CancelRequestToBorrowListingData): Result<DomainEvent, AppError> =
    match data.ListingStatus with
    | RequestedToBorrow borrowerId when borrowerId = data.RequesterId ->
        DomainEvent.RequestToBorrowCancelled { RequesterId = data.RequesterId } |> Ok
    | Borrowed borrowerId when borrowerId = data.RequesterId ->
        AppError.toDomain ListingIsAlreadyApproved
    | Available -> AppError.toDomain ListingIsNotRequested
    | RequestedToBorrow _
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation

type ApproveRequestToBorrowListingData =
    { ListingStatus: ListingStatus
      OwnerId: UserId
      ApproverId: UserId }

let approveRequestToBorrowListing (data: ApproveRequestToBorrowListingData): Result<DomainEvent, AppError> =
    if data.OwnerId <> data.ApproverId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match data.ListingStatus with
        | RequestedToBorrow borrowerId ->
            DomainEvent.RequestToBorrowApproved { BorrowerId = borrowerId } |> Ok
        | Borrowed _ -> AppError.toDomain ListingIsAlreadyBorrowed
        | Available -> AppError.toDomain ListingIsNotRequested

type ReturnBorrowedListingData =
    { ListingStatus: ListingStatus
      BorrowerId: UserId }

let returnBorrowedListing (data: ReturnBorrowedListingData): Result<DomainEvent, AppError> =
    match data.ListingStatus with
    | Borrowed borrowerId when data.BorrowerId = borrowerId ->
        DomainEvent.ListingReturned { BorrowerId = borrowerId } |> Ok
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation
    | RequestedToBorrow _
    | Available -> AppError.toDomain ListingIsNotBorrowed

let changeListingStatus (listing: BookListing) (args: ChangeListingStatusArgs) =
    match args.Command with
    | RequestToBorrow ->
        requestToBorrowListing
            { ListingStatus = listing.Status
              OwnerId = listing.OwnerId
              RequesterId = args.ChangeRequestedByUserId }
    | CancelRequestToBorrow ->
        cancelRequestToBorrowListing
            { ListingStatus = listing.Status
              RequesterId = args.ChangeRequestedByUserId }
    | ApproveRequestToBorrow ->
        approveRequestToBorrowListing
            { ListingStatus = listing.Status
              OwnerId = listing.OwnerId
              ApproverId = args.ChangeRequestedByUserId }
    | ReturnBorrowedListing ->
        returnBorrowedListing
            { ListingStatus = listing.Status
              BorrowerId = args.ChangeRequestedByUserId }