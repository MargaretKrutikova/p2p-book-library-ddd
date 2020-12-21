module Client.Pages.MyBookListings

open Api.BookListing.Models
open Client.Utils
open Client.Api

open System
open Feliz
open Elmish
open Feliz.UseElmish

type MyBookListingsApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | Data of ListingOutputModel list

type NewBookListingInputModel = {
    Author: string
    Title: string
}

type Model = {
    UserId: Guid
    MyBookListingsApiState: MyBookListingsApiState
    NewBookListing: NewBookListingInputModel
}
with
    static member CreateDefault (userId: Guid) = { 
        UserId = userId
        MyBookListingsApiState = Loading
        NewBookListing = { Title = ""; Author = "" }
    }
    
let canAddBookListing inputModel =
    inputModel.Author |> stringNotEmpty &&
    inputModel.Title |> stringNotEmpty

let toCreateListingInputModel (model: Model): ListingCreateInputModel = {
   UserId = model.UserId
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
    Model.CreateDefault userId, Cmd.OfAsync.eitherAsResult bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    
let update (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedMyBookListings data -> { model with MyBookListingsApiState = Data data }, Cmd.none
    | MyBookListingsError error -> { model with MyBookListingsApiState = Error error }, Cmd.none
    | NewBookListingInputChanged inputModel -> { model with NewBookListing = inputModel }, Cmd.none
    | AddBookListingClicked -> 
        model, Cmd.OfAsync.eitherAsResult bookListingApi.create (toCreateListingInputModel model) BookListingAdded AddBookListingError
    | BookListingAdded listing -> model, Cmd.none
    | AddBookListingError error -> model, Cmd.none

let addBookListingView inputModel dispatch =
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
                                dispatch AddBookListingClicked)
               prop.children [Html.text "Add" ] ]
        ]
   ]

let listingsView (listings: ListingOutputModel list) =
    Html.ul [
        prop.children (listings |> Seq.map (fun l -> Html.li (l.Id.ToString())) |> Seq.toList)
    ]

let view = React.functionComponent(fun (props: {| userId: Guid |}) ->
   let model, dispatch = React.useElmish(init props.userId, update, [| |])        
   match model.MyBookListingsApiState with
       | NotAsked -> Html.span []
       | Loading -> Html.text "..."
       | Error e -> Html.text "Error"
       | Data listings -> 
            Html.div [
                addBookListingView model.NewBookListing dispatch
                listingsView listings
            ]
)
