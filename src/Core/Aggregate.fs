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

type ListingAggregate = Aggregate<BookListing option, Command, DomainEvent>

let init = None
let private applyEvent (listing: BookListing) (event: DomainEvent): BookListing =
    match event with
    | DomainEvent.ListingRequestedToBorrow args -> { listing with Status = RequestedToBorrow args.RequesterId }
    | DomainEvent.RequestToBorrowCancelled _ -> { listing with Status = Available }
    | DomainEvent.RequestToBorrowApproved args -> { listing with Status = Borrowed args.BorrowerId }
    | DomainEvent.ListingReturned _ -> { listing with Status = Available }
    | _ -> listing

let apply (listingOption: BookListing option) (event: DomainEvent): BookListing option =
    match listingOption, event with
    | None, DomainEvent.BookListingPublished args -> args.Listing |> Some
    | Some listing, event -> applyEvent listing event |> Some
    | _ -> None

let execute state command =
    match command, state with
    | Command.PublishBookListing args, None ->
        Logic.publishBookListing args
            |> Result.map (fun listing -> BookListingPublished { Listing = listing } |> List.singleton)
    | Command.ChangeListingStatus args, Some listing ->
        Logic.changeListingStatus listing args |> Result.map List.singleton
    | Command.ChangeListingStatus _, None -> ListingNotFound |> AppError.toValidation
    | _ -> List.empty |> Ok

let listingAggregate = { Init = init; Apply = apply; Execute = execute }