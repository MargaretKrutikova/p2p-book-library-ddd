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
    { ListingId: ListingId
      ListingStatus: ListingStatus
      OwnerId: UserId
      RequesterId: UserId }

let requestToBorrowListing (data: RequestToBorrowListingData): Result<Event * ListingStatus, AppError> =
    if data.OwnerId = data.RequesterId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match data.ListingStatus with
        | Available ->
            let event = Event.ListingRequestedToBorrow { ListingId = data.ListingId; RequesterId = data.RequesterId }
            (event, RequestedToBorrow data.RequesterId) |> Ok
        | RequestedToBorrow borrowerId when borrowerId = data.RequesterId ->
            AppError.toDomain ListingAlreadyRequestedByUser
        | RequestedToBorrow _
        | Borrowed _ -> AppError.toDomain BorrowErrorListingIsNotAvailable

type CancelRequestToBorrowListingData =
    { ListingId: ListingId
      ListingStatus: ListingStatus
      RequesterId: UserId }

let cancelRequestToBorrowListing (data: CancelRequestToBorrowListingData): Result<Event * ListingStatus, AppError> =
    match data.ListingStatus with
    | RequestedToBorrow borrowerId when borrowerId = data.RequesterId ->
        let event = Event.RequestToBorrowCancelled { ListingId = data.ListingId; RequesterId = data.RequesterId }
        (event, Available) |> Ok
    | Borrowed borrowerId when borrowerId = data.RequesterId ->
        AppError.toDomain ListingIsAlreadyApproved
    | Available -> AppError.toDomain ListingIsNotRequested
    | RequestedToBorrow _
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation

type ApproveRequestToBorrowListingData =
    { ListingId: ListingId
      ListingStatus: ListingStatus
      OwnerId: UserId
      ApproverId: UserId }

let approveRequestToBorrowListing (data: ApproveRequestToBorrowListingData): Result<Event * ListingStatus, AppError> =
    if data.OwnerId <> data.ApproverId then
        AppError.toDomain ListingNotEligibleForOperation
    else
        match data.ListingStatus with
        | RequestedToBorrow borrowerId ->
            let event = Event.RequestToBorrowApproved { ListingId = data.ListingId; BorrowerId = borrowerId }
            (event, Borrowed borrowerId) |> Ok
        | Borrowed _ -> AppError.toDomain ListingIsAlreadyBorrowed
        | Available -> AppError.toDomain ListingIsNotRequested

type ReturnBorrowedListingData =
    { ListingId: ListingId
      ListingStatus: ListingStatus
      BorrowerId: UserId }

let returnBorrowedListing (data: ReturnBorrowedListingData): Result<Event * ListingStatus, AppError> =
    match data.ListingStatus with
    | Borrowed borrowerId when data.BorrowerId = borrowerId ->
        let event = Event.ListingReturned { ListingId = data.ListingId; BorrowerId = borrowerId }
        (event, Available) |> Ok
    | Borrowed _ -> AppError.toDomain ListingNotEligibleForOperation
    | RequestedToBorrow _
    | Available -> AppError.toDomain ListingIsNotBorrowed
