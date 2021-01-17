module Core.Events

open System
open Core.Domain.Types

type ListingRequestedToBorrowEventArgs =
    { ListingId: ListingId
      RequesterId: UserId }

type RequestToBorrowCancelledEventArgs =
    { ListingId: ListingId
      RequesterId: UserId }

type RequestToBorrowApprovedEventArgs =
    { ListingId: ListingId
      BorrowerId: UserId }

type ListingReturnedEventArgs =
    { ListingId: ListingId
      BorrowerId: UserId }

[<RequireQualifiedAccess>]
type Event =
    | BookListingPublished of ListingId
    | ListingRequestedToBorrow of ListingRequestedToBorrowEventArgs
    | RequestToBorrowCancelled of RequestToBorrowCancelledEventArgs
    | RequestToBorrowApproved of RequestToBorrowApprovedEventArgs
    | ListingReturned of ListingReturnedEventArgs

type EventEnvelope = { Timestamp: DateTime; Event: Event }
