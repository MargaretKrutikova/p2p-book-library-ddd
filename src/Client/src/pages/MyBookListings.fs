module Client.Pages.MyBookListings

open System
open Api.BookListing.Models
open Client.Utils
open Feliz
open Elmish
open Feliz.UseElmish

type MyBookListingsApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | Data of ListingOutputModel list

type Model = {
    UserId: Guid
    MyBookListingsApiState: MyBookListingsApiState
}
with
    static member CreateDefault (userId: Guid) = { UserId = userId ; MyBookListingsApiState = Loading }

type Msg =
    | ReceivedMyBookListings of ListingOutputModel list
    | MyBookListingsError of ApiError

let init (userId: Guid): Model * Cmd<Msg> =
    Model.CreateDefault userId, Cmd.OfAsync.eitherAsResult Client.Api.bookListingApi.getByUserId userId ReceivedMyBookListings MyBookListingsError
    
let update (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | ReceivedMyBookListings data -> { model with MyBookListingsApiState = Data data }, Cmd.none
    | MyBookListingsError error -> { model with MyBookListingsApiState = Error error }, Cmd.none

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
       | Data listings -> Html.div (listingsView listings)
)
