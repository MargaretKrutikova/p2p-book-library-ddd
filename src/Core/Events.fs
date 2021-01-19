module Core.Events

open Core.Domain.Types

type ListingRequestedToBorrowEventArgs = { RequesterId: UserId }
type RequestToBorrowCancelledEventArgs = { RequesterId: UserId }
type RequestToBorrowApprovedEventArgs = { BorrowerId: UserId }
type ListingReturnedEventArgs = { BorrowerId: UserId }
type BookListingPublishedEventArgs = { Listing: BookListing }

[<RequireQualifiedAccess>]
type DomainEvent =
    | BookListingPublished of BookListingPublishedEventArgs
    | ListingRequestedToBorrow of ListingRequestedToBorrowEventArgs
    | RequestToBorrowCancelled of RequestToBorrowCancelledEventArgs
    | RequestToBorrowApproved of RequestToBorrowApprovedEventArgs
    | ListingReturned of ListingReturnedEventArgs

module Projections =
    let private applyEventToExistingListing (listing: BookListing) (event: DomainEvent): BookListing =
        match event with
        | DomainEvent.ListingRequestedToBorrow args -> { listing with Status = RequestedToBorrow args.RequesterId }
        | DomainEvent.RequestToBorrowCancelled _ -> { listing with Status = Available }
        | DomainEvent.RequestToBorrowApproved args -> { listing with Status = Borrowed args.BorrowerId }
        | DomainEvent.ListingReturned _ -> { listing with Status = Available }
        | _ -> listing

    let applyEvent (listingOption: BookListing option) (event: DomainEvent): BookListing option =
        match listingOption, event with
        | None, DomainEvent.BookListingPublished args -> args.Listing |> Some
        | Some listing, event -> applyEventToExistingListing listing event |> Some
        | _ -> None