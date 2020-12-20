module Client.App

open System
open Client.Pages
open Elmish
open Elmish.React
open Feliz
open Router

type UserId = Guid

type State = {
  CurrentPage: Route
  UserId: UserId option
}

type Msg = 
    | PageChanged of Route
    | UserCreated of UserId

let init () = { CurrentPage = Route.SignUp; UserId = None }, Cmd.none

let update msg state =
    match msg with
    | PageChanged nextPage -> { state with CurrentPage = nextPage }, Cmd.none
    | UserCreated userId -> { state with CurrentPage = Route.MyBookListings; UserId = Some userId }, Cmd.none

let view model dispatch =
    let handleUserCreated id = UserCreated id |> dispatch

    let currentPage =
        match model.CurrentPage, model.UserId with
        | Route.Home, _ -> Html.h1 "Home"
        | Route.SignUp, None -> Signup.view {| onUserCreated = handleUserCreated |}
        | Route.MyBookListings, Some userId -> MyBookListings.view {| userId = userId |}
        | _ -> Html.h1 "Not Found"

    React.router [
        router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
        router.children currentPage
    ]
  
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
