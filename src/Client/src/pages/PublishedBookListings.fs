module Client.Pages.PublishedBookListings

open Api.Models
open Client.Types
open Client.Utils
open Client.Api

open System
open Core.QueryModels
open Feliz
open Elmish
open Feliz.UseElmish
open Fable.React.Props
open Fable.React
open Fulma

type Model =
    { PublishedListings: ApiState<PublishedListingsOutputModel> }

type Msg =
    | ReceivedPublishedBookListings of PublishedListingsOutputModel
    | PublishedBookListingsError of ApiError
    | RequestToBorrowBookListing of listingId: Guid
    | BookListingRequestedSuccessfully
    | BookListingRequestError of ApiError

let init (): Model * Cmd<Msg> =
    { PublishedListings = Loading },
    Cmd.OfAsync.eitherAsResult bookListingApi.getAllListings () ReceivedPublishedBookListings PublishedBookListingsError

let update (appUser: AppUser) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedPublishedBookListings data ->
        { model with PublishedListings = ApiState.Data data }, Cmd.none
    | PublishedBookListingsError error ->
        { model with PublishedListings = Error error }, Cmd.none
    | RequestToBorrowBookListing listingId ->
        match appUser with
        | Anonymous -> model, Cmd.none
        | LoggedIn user ->
            model,
            Cmd.OfAsync.eitherAsResult
                bookListingApi.requestToBorrow 
                { ListingId = listingId; BorrowerId = user.UserId }
                (fun _ -> BookListingRequestedSuccessfully)
                BookListingRequestError
    | _ -> model, Cmd.none
    
// VIEW

let addBookListingResultMessage (result: ApiState<unit>) =
    match result with
    | ApiState.Data _ -> Notification.success "Published successfully!"
    | Error _ -> Notification.error
    | NotAsked -> div [] []
    | Loading -> div [] []

let publishedBookListingView dispatch (listing: BookListingDto) =
    div [ ClassName "flex flex-column" ] [
        div [] [ 
            Icon.icon 
                [ Icon.Size IsSmall; Icon.CustomClass "ml-1 mr-2" ; Icon.Props [] ] 
                [ i [ Style [ Color "" ]; ClassName "fa fa-lg fa-book" ] [] ]
            str (listing.Title + " " + listing.Author) 
        ]
        div [ ] (
            match listing.Status with
            | Available ->
                [ str "Available, "
                  a [ ClassName "is-link is-light is-text"
                      OnClick (fun e ->
                                   e.preventDefault ()
                                   listing.ListingId |> RequestToBorrowBookListing |> dispatch 
                               ) ] [ str "request to borrow" ]
                 ]
            | Borrowed user -> [ "Borrowed by " + user.Name |> str ] 
            | RequestedToBorrow user -> [ "Waiting to borrow by " + user.Name |> str ] 
        )
    ]

let listingsView dispatch (model: PublishedListingsOutputModel) =
    match model.Listings with
    | [] -> Heading.h5 [ Heading.CustomClass "has-text-centered" ] [str "No published listings found"]
    | listings ->  
        Html.ul
            [ prop.children
                (listings
                 |> Seq.map (publishedBookListingView dispatch)
                 |> Seq.toList) ]

let view =
    React.functionComponent (fun (props: {| appUser: AppUser |}) ->
        let model, dispatch =
            React.useElmish (init (), update props.appUser, [||])
        
        let publishedListingsView =
            match model.PublishedListings with
            | NotAsked -> Html.span []
            | Loading -> Html.text "..."
            | Error _ -> Notification.error
            | ApiState.Data data -> listingsView dispatch data
        
        Column.column [ Column.Width(Screen.All, Column.IsOneThird); Column.CustomClass "" ] [
            publishedListingsView
        ])
