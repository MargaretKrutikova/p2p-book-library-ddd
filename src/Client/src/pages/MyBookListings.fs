module Client.Pages.MyBookListings

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

type NewBookListingInputModel = { Author: string; Title: string }

type Model =
    { MyBookListings: ApiState<UserListingsOutputModel>
      PublishBookListingState: ApiState<unit>
      NewBookListing: NewBookListingInputModel }
    static member CreateDefault() =
        { MyBookListings = Loading
          NewBookListing = { Title = ""; Author = "" }
          PublishBookListingState = NotAsked }

let canPublishBookListing inputModel =
    inputModel.Author
    |> stringNotEmpty
    && inputModel.Title |> stringNotEmpty

let toPublishListingInputModel (userId: Guid) (model: Model): ListingPublishInputModel =
    { UserId = userId
      Title = model.NewBookListing.Title
      Author = model.NewBookListing.Author }

let updateUserListing (updatedListing: BookListingDto) (userListing: UserBookListingDto) =
    if userListing.ListingId = updatedListing.ListingId then
        { userListing with Status = updatedListing.Status }
    else
        userListing

let updateUserListingOutputModel (updatedListing: BookListingDto) (model: UserListingsOutputModel) =
    let listings =
        model.Listings
        |> Seq.map (updateUserListing updatedListing)
        |> Seq.toList
        
    { model with Listings = listings }

type Msg =
    | ReceivedMyBookListings of UserListingsOutputModel
    | MyBookListingsError of ApiError
    | NewBookListingInputChanged of NewBookListingInputModel
    // publish
    | PublishBookListingClicked
    | BookListingPublished of ListingPublishedOutputModel
    | PublishBookListingError of ApiError
    // approve
    | ApproveRequestToBorrow of listingId: Guid
    | RequestToBorrowApproved of updatedListing: BookListingDto option
    | ApproveRequestToBorrowError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    Model.CreateDefault(),
    Cmd.OfAsync.eitherAsResult listingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError

let update (userId: Guid) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedMyBookListings data ->
        { model with MyBookListings = ApiState.Data data },
        Cmd.none
    | MyBookListingsError error ->
        { model with MyBookListings = Error error },
        Cmd.none
    | NewBookListingInputChanged inputModel ->
        { model with NewBookListing = inputModel },
        Cmd.none
    | PublishBookListingClicked ->
        let publishBookListingModel = toPublishListingInputModel userId model
        model,
        Cmd.OfAsync.eitherAsResult listingApi.publish publishBookListingModel BookListingPublished PublishBookListingError
    | BookListingPublished _ ->
        { model with MyBookListings = Loading },
        Cmd.OfAsync.eitherAsResult listingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    | ApproveRequestToBorrow listingId ->
        let approveModel =  { ListingId = listingId; Command = ChangeListingStatusInputCommand.ApproveRequestToBorrow; UserId = userId }
        model,
        Cmd.OfAsync.eitherAsResult listingApi.changeListingStatus approveModel RequestToBorrowApproved ApproveRequestToBorrowError
    | RequestToBorrowApproved (Some updatedListing) ->
        let apiState = updateApiState (updateUserListingOutputModel updatedListing) model.MyBookListings
        { model with MyBookListings = apiState }, Cmd.none

    | _ -> model, Cmd.none
// VIEW

let addBookListingResultMessage (result: ApiState<unit>) =
    match result with
    | ApiState.Data _ -> Notification.success "Published successfully!"
    | Error _ -> Notification.error
    | NotAsked -> div [] []
    | Loading -> div [] []

let publishBookListingView (model: Model) dispatch =
    let inputModel = model.NewBookListing
    let updateAuthor str: NewBookListingInputModel = { inputModel with Author = str }
    let updateTitle str: NewBookListingInputModel = { inputModel with Title = str }

    Columns.columns [ Columns.IsCentered ] [
        Column.column [ Column.Width(Screen.All, Column.IsOneThird) ] [
            Heading.h3 [] [ str "Publish book" ]
            Box.box' [] [
                form [] [
                    Field.div [] [
                        Label.label [] [ str "Author" ]
                        Control.div [] [
                            Input.text [ Input.Value inputModel.Author
                                         Input.OnChange
                                             (eventToInputValue
                                              >> updateAuthor
                                              >> NewBookListingInputChanged
                                              >> dispatch) ]
                        ]
                    ]
                    Field.div [] [
                        Label.label [] [ str "Title" ]
                        Control.div [] [
                            Input.text [ Input.Value inputModel.Title
                                         Input.OnChange
                                             (eventToInputValue
                                              >> updateTitle
                                              >> NewBookListingInputChanged
                                              >> dispatch) ]
                        ]
                    ]

                    Field.div [] [
                        Control.div [] [
                            addBookListingResultMessage model.PublishBookListingState
                        ]
                    ]
                    Field.div [] [
                        Control.div [] [
                            Button.button [ Button.Disabled(canPublishBookListing inputModel |> not)
                                            Button.Color IsPrimary
                                            Button.IsFullWidth
                                            Button.OnClick(fun e ->
                                                e.preventDefault ()
                                                dispatch PublishBookListingClicked) ] [
                                str "Publish"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let myBookListingView dispatch (listing: UserBookListingDto) =
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
                [ str "Available" ]
            | Borrowed user -> [ "Borrowed by " + user.Name |> str ] 
            | RequestedToBorrow user ->
                  [ "Requested by " + user.Name + ", " |> str 
                    a [ ClassName "is-link is-light is-text"
                        OnClick (fun e ->
                                   e.preventDefault ()
                                   listing.ListingId |> ApproveRequestToBorrow |> dispatch 
                               ) ] [ str "approve request" ] ] 
        )
    ]

let listingsView dispatch (model: UserListingsOutputModel) =
    Columns.columns [ Columns.IsCentered ] [
        Column.column [ Column.Width(Screen.All, Column.IsOneThird) ] [
            Html.ul
                [ prop.children
                    (model.Listings
                     |> Seq.map (myBookListingView dispatch)
                     |> Seq.toList) ]
        ]
    ]

let view =
    React.functionComponent (fun (props: {| userId: Guid |}) ->
        let model, dispatch =
            React.useElmish (init props.userId, update props.userId, [||])
        
        let allListingsView =
            match model.MyBookListings with
            | ApiState.NotAsked -> Html.span []
            | Loading -> Html.text "..."
            | Error _ -> Notification.error
            | ApiState.Data data -> listingsView dispatch data
        
        Column.column [ Column.Width(Screen.All, Column.IsFull) ] [
            publishBookListingView model dispatch
            allListingsView
        ])
