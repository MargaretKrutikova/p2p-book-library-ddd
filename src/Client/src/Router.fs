module Client.Router

open Feliz.Router

[<RequireQualifiedAccess>]
type Route =
  | Home
  | SignUp
  | SignIn
  | AllBookListings
  | MyBookListings
  | MyActivity
  | NotFound

let parseUrl = function
  | [ ] -> Route.Home
  | [ "signin" ] -> Route.SignIn
  | [ "signup" ] -> Route.SignUp
  | [ "listings" ] -> Route.AllBookListings
  | [ "my-listings" ] -> Route.MyBookListings
  | [ "my-activity" ] -> Route.MyActivity
  | _ -> Route.NotFound

let urlToRoute = function
  | Route.Home -> ""
  | Route.SignIn -> "signin"
  | Route.SignUp -> "signup"
  | Route.AllBookListings -> "listings"
  | Route.MyBookListings -> "my-listings"
  | Route.MyActivity -> "my-activity"
  | _ -> ""

let navigateToMyBookListings () =
    Route.MyBookListings |> urlToRoute |> Router.navigate

let navigateToSignIn () =
    Route.SignIn |> urlToRoute |> Router.navigate

let canViewIfLoggedIn route =
    match route with
    | Route.MyBookListings -> true
    | Route.AllBookListings -> true
    | Route.MyActivity -> true
    | _ -> false

let canViewIfLoggedOut =
    function 
    | Route.MyBookListings -> false
    | Route.MyActivity -> false
    | _ -> true

let loggedInPageOrDefault page =
    if canViewIfLoggedIn page then page else Route.Home

let anonymousPageOrDefault page =
    if canViewIfLoggedOut page then page else Route.SignIn
