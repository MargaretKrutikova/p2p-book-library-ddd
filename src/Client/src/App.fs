module Client.App

open System
open Client.Pages
open Elmish
open Elmish.React
open Feliz
open Router

type State = {
  CurrentPage: Route
}

type Msg = PageChanged of Route

let init () = { CurrentPage = Route.SignUp }, Cmd.none

let update msg state =
    match msg with
    | PageChanged nextPage -> { state with CurrentPage = nextPage }, Cmd.none

let view model dispatch =
    let currentPage =
        match model.CurrentPage with
        | Route.Home -> Html.h1 "Home"
        | Route.SignUp -> Signup.view ()
        | Route.MyBookListings -> MyBookListings.view {| userId = Guid.Empty |}
        | _ -> Html.h1 "Not Found"

    React.router [
        router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
        router.children currentPage
    ]
  
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
