module Core.Aggregate

open Core.Commands
open Core.Domain.Errors
open Core.Domain.Types
open Core.Events

type Aggregate<'state, 'command, 'event> = {
    Init : 'state
    Apply: 'state -> 'event -> 'state
    Execute: 'state -> 'command -> Result<'event list, AppError>
}

type ListingAggregateState =
    | Existing of BookListing
    | NonExisting

type ListingAggregate = Aggregate<ListingAggregateState, Command, DomainEvent>

let init = NonExisting
let applyEvent (listing: BookListing) (event: DomainEvent): BookListing =
    match event with
    | DomainEvent.ListingRequestedToBorrow args -> { listing with Status = RequestedToBorrow args.RequesterId }
    | DomainEvent.RequestToBorrowCancelled _ -> { listing with Status = Available }
    | DomainEvent.RequestToBorrowApproved args -> { listing with Status = Borrowed args.BorrowerId }
    | DomainEvent.ListingReturned _ -> { listing with Status = Available }
    | _ -> listing

let apply (state: ListingAggregateState) (event: DomainEvent): ListingAggregateState =
    match state, event with
    | NonExisting, DomainEvent.BookListingPublished args -> args.Listing |> Existing
    | Existing listing, event -> applyEvent listing event |> Existing
    | _ -> NonExisting

let execute state command =
    match command, state with
    | Command.PublishBookListing args, NonExisting ->
        Logic.publishBookListing args
            |> Result.map (fun listing -> BookListingPublished { Listing = listing } |> List.singleton)
    | Command.ChangeListingStatus args, Existing listing ->
        Logic.changeListingStatus listing args |> Result.map List.singleton
    | Command.ChangeListingStatus _, NonExisting -> ListingNotFound |> AppError.toValidation
    | _ -> List.empty |> Ok

let listingAggregate: ListingAggregate = { Init = init; Apply = apply; Execute = execute }