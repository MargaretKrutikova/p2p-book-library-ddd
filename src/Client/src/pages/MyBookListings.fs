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

type Msg =
    | ReceivedMyBookListings of UserListingsOutputModel
    | MyBookListingsError of ApiError
    | NewBookListingInputChanged of NewBookListingInputModel
    | PublishBookListingClicked
    | BookListingPublished of ListingPublishedOutputModel
    | PublishBookListingError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    Model.CreateDefault(),
    Cmd.OfAsync.eitherAsResult bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError

let update (userId: Guid) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedMyBookListings data ->
        { model with
              MyBookListings = ApiState.Data data },
        Cmd.none
    | MyBookListingsError error ->
        { model with
              MyBookListings = Error error },
        Cmd.none
    | NewBookListingInputChanged inputModel ->
        { model with
              NewBookListing = inputModel },
        Cmd.none
    | PublishBookListingClicked ->
        let publishBookListingModel = toPublishListingInputModel userId model
        model,
        Cmd.OfAsync.eitherAsResult
            bookListingApi.publish
            publishBookListingModel
            BookListingPublished
            PublishBookListingError
    | BookListingPublished _ ->
        { model with MyBookListings = Loading },
        Cmd.OfAsync.eitherAsResult bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    | PublishBookListingError _ -> model, Cmd.none

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

let myBookListingView (listing: UserListingOutputModel) =
    Columns.columns [ Columns.IsCentered
                      Columns.IsGapless ] [
        Column.column [ Column.Width(Screen.All, Column.IsOneThird) ] [
            Icon.icon [ Icon.Size IsSmall
                        Icon.CustomClass "ml-1 mr-2"
                        Icon.Props [] ] [
                i [ Style [ Color "" ]
                    ClassName "fa fa-lg fa-book" ] []
            ]
            str (listing.Title + listing.Author) 
        ]
    ]

let listingsView (listings: UserListingOutputModel list) =
    Html.ul
        [ prop.children
            (listings
             |> Seq.map myBookListingView
             |> Seq.toList) ]

let view =
    React.functionComponent (fun (props: {| userId: Guid |}) ->
        let model, dispatch =
            React.useElmish (init props.userId, update props.userId, [||])
        
        let allListingsView =
            match model.MyBookListings with
            | ApiState.NotAsked -> Html.span []
            | Loading -> Html.text "..."
            | Error _ -> Notification.error
            | ApiState.Data data -> listingsView data.Listings
        
        Column.column [ Column.Width(Screen.All, Column.IsFull) ] [
            publishBookListingView model dispatch
            allListingsView
        ])
