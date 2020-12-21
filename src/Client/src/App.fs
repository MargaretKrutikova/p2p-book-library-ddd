module Client.App

open System
open Api.BookListing.Models
open Client.Pages
open Elmish
open Elmish.React
open Feliz
open Router

type UserId = Guid

type State = {
  CurrentPage: Route
  LoggedInUser: UserOutputModel option
}

type Msg = 
    | PageChanged of Route
    | UserCreated
    | UserLoggedIn of UserOutputModel

let init () = 
    let initialUrl = parseUrl (Router.currentUrl())
    { CurrentPage = initialUrl; LoggedInUser = None }, Cmd.none

let update msg state =
    match msg with    
    | PageChanged nextPage ->
        let allowedPage =
             match state.LoggedInUser with
             | Some _ -> loggedInPageOrDefault nextPage
             | None -> loggedOutPageOrDefault nextPage
        { state with CurrentPage = allowedPage }, Cmd.none
    | UserCreated -> 
        state, Cmd.ofSub (fun _ -> navigateToSignIn ())
    | UserLoggedIn user ->
        // TODO: save cookies with the logged in user name
        { state with LoggedInUser = Some user }, Cmd.ofSub (fun _ -> navigateToMyBookListings ())
        
let view model (dispatch: Msg -> unit) =
    let handleUserCreated _ = dispatch UserCreated
    let handleUserLoggedIn user = UserLoggedIn user |> dispatch

    let currentPage =
        match model.CurrentPage, model.LoggedInUser with
        | Route.Home, _ -> Html.h1 "Home"
        | Route.SignUp, None -> Signup.view {| onUserCreated = handleUserCreated |}
        | Route.SignIn, None -> Signin.view {| onUserLoggedIn = handleUserLoggedIn |}
        | Route.MyBookListings, Some user -> MyBookListings.view {| userId = user.UserId |}
        | _ -> Html.h1 "Not Found"

    React.router [
        router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
        router.children currentPage
    ]
  
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
