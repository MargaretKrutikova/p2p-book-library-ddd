module Client.App

open System
open Api.Models
open Client.Pages
open Elmish
open Elmish.React
open Feliz
open Router
open Fulma
open Fable.React
open Fable.React.Props

type UserId = Guid

type AppUser =
    | Anonymous
    | LoggedIn of UserOutputModel

type State =
    { CurrentPage: Route
      AppUser: AppUser }

type Msg =
    | PageChanged of Route
    | UserCreated
    | UserLoggedIn of UserOutputModel
    | NavigateToRoute of Route

type Dispatch = Msg -> unit

let redirectIfProtected (user: AppUser) (route: Route): Route =
    match route, user with
    | Route.MyBookListings, Anonymous -> Route.SignIn
    | _ -> route

let init () =
    let initialUrl = parseUrl (Router.currentUrl ())
    { CurrentPage = initialUrl
      AppUser = Anonymous },
    Cmd.none

let update msg state =
    match msg with
    | PageChanged nextPage ->
        let allowedPage =
            match state.AppUser with
            | LoggedIn _ -> loggedInPageOrDefault nextPage
            | Anonymous -> loggedOutPageOrDefault nextPage

        { state with CurrentPage = allowedPage }, Cmd.none
    | UserCreated -> state, Cmd.ofSub (fun _ -> navigateToSignIn ())
    | NavigateToRoute route ->
        state,
        Cmd.ofSub (fun _ ->
            route
            |> redirectIfProtected state.AppUser
            |> urlToRoute
            |> Router.navigate)
    | UserLoggedIn user ->
        // TODO: save cookies with the logged in user name
        { state with
              AppUser = LoggedIn user },
        Cmd.ofSub (fun _ -> navigateToMyBookListings ())

// VIEW

let navbarView (appUser: AppUser) (dispatch: Dispatch) =
    Navbar.navbar [ Navbar.CustomClass "mb-4" ] [
        Navbar.Link.a [ Navbar.Link.IsArrowless
                        Navbar.Link.Props [ OnClick(fun _ -> NavigateToRoute Route.Home |> dispatch) ] ] [
            str "Home"
        ]
        Navbar.Link.a [ Navbar.Link.IsArrowless
                        Navbar.Link.Props [] ] [
            str "All books"
        ]

        Navbar.End.div [] [
            Navbar.Item.div
                []
                (match appUser with
                 | Anonymous ->
                     [ div [ ClassName "buttons" ] [
                         Button.button [ Button.OnClick(fun _ -> NavigateToRoute Route.SignIn |> dispatch)
                                         Button.Color IsPrimary
                                         Button.IsLight ] [
                             str "Sign in"
                         ]
                         Button.button [ Button.Color IsWhite ] [
                             str "Sign up"
                         ]
                       ] ]
                 | LoggedIn user ->
                     [ Button.button [ Button.Color IsPrimary
                                       Button.IsLight
                                       Button.OnClick(fun _ -> NavigateToRoute Route.MyBookListings |> dispatch) ] [
                         str "My book listings"
                       ]

                       Navbar.Link.a [ Navbar.Link.IsArrowless ] [
                           "Logged in as " + user.Name |> str
                       ] ])
        ]
    ]


let containerView (appUser: AppUser) (dispatch: Dispatch) (children: ReactElement) =
    Container.container [ Container.IsWideScreen ] [
        navbarView appUser dispatch
        Columns.columns [ Columns.IsCentered
                          Columns.IsGapless ] [
            children
        ]
    ]


let view model (dispatch: Msg -> unit) =
    let handleUserCreated _ = dispatch UserCreated
    let handleUserLoggedIn user = UserLoggedIn user |> dispatch

    let currentPage =
        match model.CurrentPage, model.AppUser with
        | Route.Home, _ -> Html.h1 "Home"
        | Route.SignUp, Anonymous -> Signup.view {| onUserCreated = handleUserCreated |}
        | Route.SignIn, Anonymous -> Signin.view {| onUserLoggedIn = handleUserLoggedIn |}
        | Route.MyBookListings, LoggedIn user -> MyBookListings.view {| userId = user.UserId |}
        | _ -> Html.h1 "Not Found"

    React.router [ router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
                   router.children [ containerView model.AppUser dispatch currentPage ] ]

Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
