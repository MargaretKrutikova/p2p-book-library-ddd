module Client.Pages.MyActivity

open Api.Models
open Client.Utils
open Client.Api

open System
open Feliz
open Elmish
open Feliz.UseElmish
open Fable.React.Props
open Fable.React
open Fulma
open Services.QueryModels

type Model =
    { UserActivity: ApiState<UserActivity> }

type Msg =
    | ReceivedUserActivity of UserActivity
    | UserActivityError of ApiError
    // cancel request
    | CancelRequestToBorrow of listingId: Guid
    | RequestToBorrowCanceled of updatedListing: BookListingDto option
    | CancelRequestToBorrowError of ApiError
    // return
    | ReturnListing of listingId: Guid
    | ListingReturned of updatedListing: BookListingDto option
    | ReturnListingError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    { UserActivity = Loading },
    Cmd.OfAsync.eitherAsResult listingApi.getUserActivity userId ReceivedUserActivity UserActivityError

let removeListingFromUserListings listingId (listings: UserActivityListing list) =
    listings |> List.filter (fun l -> l.ListingId <> listingId)

let updateActivity (updateListings) (activity: UserActivity) =
    { activity with Listings = updateListings activity.Listings }

let removeListingFromModel (listingIdToRemove: Guid) model =
    let updateAfterRequest = removeListingFromUserListings listingIdToRemove |> updateActivity
    let apiState = updateApiState updateAfterRequest model.UserActivity
        
    { model with UserActivity = apiState }

let update (userId: Guid) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedUserActivity data ->
        { model with UserActivity = ApiState.Data data }, Cmd.none
    | UserActivityError error ->
        { model with UserActivity = Error error }, Cmd.none
    | CancelRequestToBorrow listingId ->
        let input = { ListingId = listingId; Command = ChangeListingStatusInputCommand.CancelRequestToBorrow; UserId = userId }
        model,
        Cmd.OfAsync.eitherAsResult listingApi.changeListingStatus input RequestToBorrowCanceled CancelRequestToBorrowError
    | RequestToBorrowCanceled (Some updatedListing) ->
        removeListingFromModel updatedListing.ListingId model, Cmd.none
    | ReturnListing listingId ->
        let input = { ListingId = listingId; Command = ChangeListingStatusInputCommand.ReturnListing; UserId = userId }
        model,
        Cmd.OfAsync.eitherAsResult listingApi.changeListingStatus input ListingReturned ReturnListingError
    | ListingReturned (Some updatedListing) ->
         removeListingFromModel updatedListing.ListingId model, Cmd.none

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
            | BorrowedByUser ->
                [ "Borrowed by me, " |> str
                  a [ ClassName "is-link is-light is-text"
                      OnClick (fun e ->
                                   e.preventDefault ()
                                   ReturnListing listing.ListingId |> dispatch
                               ) ] [ str "return" ]
                   ] 
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
