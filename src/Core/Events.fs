module Core.Events

open System

type ListingRequestedToBorrowEventArgs = { RequesterId: Guid }
type RequestToBorrowCancelledEventArgs = { RequesterId: Guid }
type RequestToBorrowApprovedEventArgs = { BorrowerId: Guid }
type ListingReturnedEventArgs = { BorrowerId: Guid }

type BookListingPublishedEventArgs = {
    ListingId: Guid
    OwnerId: Guid
    Author: string
    Title: string
}
type DomainEvent =
    | BookListingPublished of BookListingPublishedEventArgs
    | ListingRequestedToBorrow of ListingRequestedToBorrowEventArgs
    | RequestToBorrowCancelled of RequestToBorrowCancelledEventArgs
    | RequestToBorrowApproved of RequestToBorrowApprovedEventArgs
    | ListingReturned of ListingReturnedEventArgs

type EventEnvelope = {
    ListingId: Guid
    CreatedAtUtc: DateTime
    Event: DomainEvent
}