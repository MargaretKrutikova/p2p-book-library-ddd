module Core.Aggregate

open Core.Commands
open Core.Domain.Errors
open Core.Domain.Types
open Core.Events
open FsToolkit.ErrorHandling.ResultCE
open FsToolkit.ErrorHandling

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
    | DomainEvent.ListingRequestedToBorrow args ->
        { listing with Status = args.RequesterId |> UserId.create |> RequestedToBorrow }
    | DomainEvent.RequestToBorrowCancelled _ -> { listing with Status = Available }
    | DomainEvent.RequestToBorrowApproved args ->
        { listing with Status = args.BorrowerId |> UserId.create |> Borrowed }
    | DomainEvent.ListingReturned _ -> { listing with Status = Available }
    | _ -> listing

let private toBookListing (listingDto: BookListingPublishedEventArgs): Result<BookListing, _> =
    result {
        let! title = listingDto.Title |> Title.create
        let! author = listingDto.Author |> Author.create
        
        return {
            ListingId = listingDto.ListingId |> ListingId.create
            OwnerId = listingDto.OwnerId |> UserId.create
            Author = author
            Title = title
            Status = Available
        }
    }
   
let private toBookListingPublishedEvent (listing: BookListing): BookListingPublishedEventArgs =
    {
        ListingId = listing.ListingId |> ListingId.value
        OwnerId = listing.OwnerId |> UserId.value
        Author = listing.Author |> Author.value
        Title = listing.Title |> Title.value
    }

let apply (state: ListingAggregateState) (event: DomainEvent): ListingAggregateState =
    match state, event with
    | NonExisting, DomainEvent.BookListingPublished args ->
        args |> toBookListing |> Result.map Existing |> Result.defaultValue NonExisting
    | Existing listing, event -> applyEvent listing event |> Existing
    | _ -> NonExisting

let execute state command =
    match command, state with
    | Command.PublishBookListing args, NonExisting ->
        Logic.publishBookListing args
            |> Result.map (toBookListingPublishedEvent >> BookListingPublished >> List.singleton)
    | Command.ChangeListingStatus args, Existing listing ->
        Logic.changeListingStatus listing args |> Result.map List.singleton
    | Command.ChangeListingStatus _, NonExisting -> ListingNotFound |> AppError.toValidation
    | _ -> List.empty |> Ok

let listingAggregate: ListingAggregate = { Init = init; Apply = apply; Execute = execute }