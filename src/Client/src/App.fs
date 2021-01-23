module Client.App

open Api.Models
open Client.Types
open Client.Pages
open Elmish
open Elmish.React
open Feliz
open Router
open Fulma
open Fable.React
open Fable.React.Props
open Client.Utils

type State =
    { CurrentPage: Route
      AppUser: AppUser }

type Msg =
    | PageChanged of Route
    | UserCreated
    | NavigateToRoute of Route
    | UserLoggedIn of UserOutputModel
    | UserLoginError of ApiError
    
type Dispatch = Msg -> unit

let redirectIfProtected (user: AppUser) (route: Route): Route =
    match user with
    | Anonymous -> anonymousPageOrDefault route
    | LoggedIn _ -> loggedInPageOrDefault route

let init () =
    let initialUrl = parseUrl (Router.currentUrl ())
    let userName = User.getLoggedInUserNameFromStorage ()
    
    { CurrentPage = initialUrl; AppUser = Anonymous },
        match userName with
        | None -> Cmd.none
        | Some login ->
            Cmd.OfAsync.eitherAsResult Api.userApi.login {Name = login} UserLoggedIn UserLoginError

let update msg state =
    match msg with
    | PageChanged nextPage ->
        let allowedPage =
            nextPage |> redirectIfProtected state.AppUser

        { state with CurrentPage = allowedPage }, Cmd.none
    | UserCreated -> state, Cmd.ofSub (fun _ -> navigateToSignIn ())
    | NavigateToRoute route -> state, route |> urlToRoute |> Cmd.navigate
    | UserLoggedIn user ->
        { state with AppUser = LoggedIn user }, Cmd.ofSub (fun _ ->
            User.saveLoggedInUserNameInStorage user.Name
            navigateToMyBookListings ())
    | _ -> state, Cmd.none

// VIEW

let navbarView (appUser: AppUser) (dispatch: Dispatch) =
    Navbar.navbar [ Navbar.CustomClass "mb-4" ] [
        Navbar.Start.div [] [
            Navbar.Link.a
                [ Navbar.Link.IsArrowless
                  Navbar.Link.Props [ OnClick(fun _ -> NavigateToRoute Route.Home |> dispatch) ] ]
                [ str "Home" ]
            Navbar.Link.a
                [ Navbar.Link.IsArrowless
                  Navbar.Link.Props [ OnClick(fun _ -> NavigateToRoute Route.AllBookListings |> dispatch) ] ]
                [ str "All books" ]
        ]

        Navbar.End.div [] [
            Navbar.Item.div
                []
                (match appUser with
                 | Anonymous ->
                     [ div [ ClassName "buttons" ] [
                         Button.button [ Button.OnClick(fun _ -> NavigateToRoute Route.SignIn |> dispatch)
                                         Button.IsLight ] [
                             str "Log in"
                         ]
                         Button.button [ Button.OnClick(fun _ -> NavigateToRoute Route.SignUp |> dispatch)
                                         Button.Color IsPrimary ] [
                             str "Sign up"
                         ]
                       ] ]
                 | LoggedIn user ->
                     [ Button.button [ Button.Color IsPrimary
                                       Button.IsLight
                                       Button.CustomClass "mr-3"
                                       Button.OnClick(fun _ -> NavigateToRoute Route.MyBookListings |> dispatch) ] [
                         str "My books"
                       ]
                       Button.button [ Button.Color IsGrey
                                       Button.OnClick(fun _ -> NavigateToRoute Route.MyActivity |> dispatch) ] [
                         str "My activity"
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
        | Route.AllBookListings, appUser -> PublishedBookListings.view {| appUser = appUser |}
        | Route.MyActivity, LoggedIn user -> MyActivity.view {| userId = user.UserId |}        
        | _ -> Html.h1 "Not Found"

    React.router [ router.onUrlChanged (parseUrl >> PageChanged >> dispatch)
                   router.children [ containerView model.AppUser dispatch currentPage ] ]

Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
