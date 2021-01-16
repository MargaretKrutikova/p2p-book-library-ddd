module Client.Pages.MyActivity

open Api.Models
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
    { UserActivity: ApiState<UserActivity> }

type Msg =
    | ReceivedUserActivity of UserActivity
    | UserActivityError of ApiError
    | CancelRequestToBorrow of listingId: Guid
    | RequestToBorrowCanceled of updatedListing: BookListingDto option
    | CancelRequestToBorrowError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    { UserActivity = Loading },
    Cmd.OfAsync.eitherAsResult listingApi.getUserActivity userId ReceivedUserActivity UserActivityError

let removeListingFromUserListings listingId (listings: UserActivityListing list) =
    listings |> List.filter (fun l -> l.ListingId <> listingId)

let updateActivity (updateListings) (activity: UserActivity) =
    { activity with Listings = updateListings activity.Listings }

let update (userId: Guid) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedUserActivity data ->
        { model with UserActivity = ApiState.Data data }, Cmd.none
    | UserActivityError error ->
        { model with UserActivity = Error error }, Cmd.none
    | CancelRequestToBorrow listingId ->
        model,
        Cmd.OfAsync.eitherAsResult
            listingApi.changeListingStatus 
            { ListingId = listingId; Command = ChangeListingStatusInputCommand.CancelRequestToBorrow; UserId = userId }
            RequestToBorrowCanceled
            CancelRequestToBorrowError
    | RequestToBorrowCanceled (Some updatedListing) ->
        let updateAfterRequest = removeListingFromUserListings updatedListing.ListingId |> updateActivity
        let apiState = updateApiState updateAfterRequest model.UserActivity
        
        { model with UserActivity = apiState }, Cmd.none

    | _ -> model, Cmd.none
// VIEW

let addBookListingResultMessage (result: ApiState<unit>) =
    match result with
    | ApiState.Data _ -> Notification.success "Published successfully!"
    | Error _ -> Notification.error
    | NotAsked -> div [] []
    | Loading -> div [] []

let userActivityListingView dispatch (listing: UserActivityListing) =
    div [ ClassName "flex flex-column" ] [
        div [] [ 
            Icon.icon 
                [ Icon.Size IsSmall; Icon.CustomClass "ml-1 mr-2" ; Icon.Props [] ] 
                [ i [ Style [ Color "" ]; ClassName "fa fa-lg fa-book" ] [] ]
            str (listing.Title + " " + listing.Author) 
        ]
        div [ ] (
            match listing.Status with
            | RequestedByUser ->
                [ str "Requested by me, "
                  a [ ClassName "is-link is-light is-text"
                      OnClick (fun e ->
                                   e.preventDefault ()
                                   CancelRequestToBorrow listing.ListingId |> dispatch
                               ) ] [ str "cancel" ]
                 ]
            | BorrowedByUser -> [ "Borrowed by me" |> str ] 
        )
    ]

let userActivityView dispatch (model: UserActivity) =
    match model.Listings with
    | [] -> Heading.h5 [ Heading.CustomClass "has-text-centered" ] [str "Your activity is empty!"]
    | listings ->  
        Html.ul
            [ prop.children
                (listings
                 |> Seq.map (userActivityListingView dispatch)
                 |> Seq.toList) ]

let view =
    React.functionComponent (fun (props: {| userId: Guid |}) ->
        let model, dispatch =
            React.useElmish (init props.userId, update props.userId, [||])
        
        let listingsView =
            match model.UserActivity with
            | NotAsked -> Html.span []
            | Loading -> Html.text "..."
            | Error _ -> Notification.error
            | ApiState.Data data -> userActivityView dispatch data
        
        Column.column [ Column.Width(Screen.All, Column.IsOneThird); Column.CustomClass "" ] [
            listingsView
        ])
