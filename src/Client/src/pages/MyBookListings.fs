module Client.Pages.MyBookListings

open Api.BookListing.Models
open Api.BookListing.SignalRHub
open Client.Utils
open Client.Api

open System
open Feliz
open Elmish
open Feliz.UseElmish
open Fable.SignalR.Feliz
open Fable.SignalR
open Fable.SignalR.Elmish

type MyBookListingsApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | Data of ListingOutputModel list

type NewBookListingInputModel = {
    Author: string
    Title: string
}

type Model =
    {
        MyBookListings: MyBookListingsApiState
        NewBookListing: NewBookListingInputModel
        Hub: Elmish.Hub<Action,Response> option }
    interface System.IDisposable with
        member this.Dispose () =
            this.Hub |> Option.iter (fun hub -> hub.Dispose())
    static member CreateDefault () = { 
            MyBookListings = Loading
            NewBookListing = { Title = ""; Author = "" }
            Hub = None
        }
    
let canAddBookListing inputModel =
    inputModel.Author |> stringNotEmpty &&
    inputModel.Title |> stringNotEmpty

let toCreateListingInputModel (userId: Guid) (model: Model): ListingCreateInputModel = {
   UserId = userId
   Title = model.NewBookListing.Title
   Author = model.NewBookListing.Author
}

type Msg =
    | ReceivedMyBookListings of ListingOutputModel list
    | MyBookListingsError of ApiError
    | NewBookListingInputChanged of NewBookListingInputModel
    | AddBookListingClicked
    | BookListingAdded of ListingCreatedOutputModel
    | AddBookListingError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    Model.CreateDefault (), Cmd.OfAsync.eitherAsResult bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    
let update (userId: Guid) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedMyBookListings data -> { model with MyBookListings = Data data }, Cmd.none
    | MyBookListingsError error -> { model with MyBookListings = Error error }, Cmd.none
    | NewBookListingInputChanged inputModel -> { model with NewBookListing = inputModel }, Cmd.none
    | AddBookListingClicked -> 
        // let addBookListingModel = toCreateListingInputModel userId model
        model, Cmd.none // Cmd.ofSub (BookListingSignalRAction.CreateBookListing addBookListingModel)
        //Cmd.OfAsync.eitherAsResult bookListingApi.create addBookListingModel BookListingAdded AddBookListingError
    | BookListingAdded _ -> 
        { model with MyBookListings = Loading }, Cmd.OfAsync.eitherAsResult bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    | AddBookListingError _ -> model, Cmd.none

let addBookListingView (hub: Hub<BookListingSignalRAction, Response>) userId model dispatch =
    let inputModel = model.NewBookListing
    let updateAuthor str: NewBookListingInputModel = { inputModel with Author = str }
    let updateTitle str: NewBookListingInputModel = { inputModel with Title = str }

    Html.div [
       Html.h1 [ prop.children [ Html.text "Add book listing" ] ]
       Html.form [
           Html.input [
                   prop.onChange (eventToInputValue >> updateAuthor >> NewBookListingInputChanged >> dispatch)
                   prop.value inputModel.Author
                   prop.type' "Text"
                   prop.placeholder "Author"
               ]
           Html.input [
                   prop.onChange (eventToInputValue >> updateTitle >> NewBookListingInputChanged >> dispatch)
                   prop.value inputModel.Title
                   prop.type' "Text"
                   prop.placeholder "Title"
               ]
           Html.button [ 
               prop.type' "submit"
               prop.onClick (fun e -> 
                                e.preventDefault ()
                                toCreateListingInputModel userId model |> BookListingSignalRAction.CreateBookListing |> hub.send |> ignore
                                )
               prop.children [Html.text "Add" ] ]
        ]
   ]

let listingsView (listings: ListingOutputModel list) =
    Html.ul [
        prop.children (listings |> Seq.map (fun l -> Html.li (l.Id.ToString())) |> Seq.toList)
    ]

let view = React.functionComponent(fun (props: {| userId: Guid |}) ->
   let model, dispatch = React.useElmish(init props.userId, update props.userId, [| |])
   let hub =
        React.useSignalR<BookListingSignalRAction, Response>(fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .onMessage <|
                    function
                    | Response.MyListings listings -> ReceivedMyBookListings listings |> dispatch
        )
   match model.MyBookListings with
       | NotAsked -> Html.span []
       | Loading -> Html.text "..."
       | Error e -> Html.text "Error"
       | Data listings -> 
            Html.div [
                addBookListingView hub.current props.userId model dispatch
                listingsView listings
            ]
)
