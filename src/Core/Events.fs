module Core.Events

open Core.Domain.Types

type ListingRequestedToBorrowEventArgs = { RequesterId: UserId }
type RequestToBorrowCancelledEventArgs = { RequesterId: UserId }
type RequestToBorrowApprovedEventArgs = { BorrowerId: UserId }
type ListingReturnedEventArgs = { BorrowerId: UserId }
type BookListingPublishedEventArgs = { Listing: BookListing }

type DomainEvent =
    | BookListingPublished of BookListingPublishedEventArgs
    | ListingRequestedToBorrow of ListingRequestedToBorrowEventArgs
    | RequestToBorrowCancelled of RequestToBorrowCancelledEventArgs
    | RequestToBorrowApproved of RequestToBorrowApprovedEventArgs
    | ListingReturned of ListingReturnedEventArgs
