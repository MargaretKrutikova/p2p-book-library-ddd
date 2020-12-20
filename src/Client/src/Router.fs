module Client.Router

[<RequireQualifiedAccess>]
type Route =
  | Home
  | SignUp
  | SignIn
  | AllBookListings
  | MyBookListings
  | NotFound

let parseUrl = function
  | [ ] -> Route.Home
  | [ "sign-in" ] -> Route.SignIn
  | [ "sign-up" ] -> Route.SignUp
  | [ "listings" ] -> Route.AllBookListings
  | [ "my-pages" ] -> Route.MyBookListings
  | _ -> Route.NotFound

let urlToRoute = function
  | Route.Home -> ""
  | Route.SignIn -> "sign-in"
  | Route.SignUp -> "sign-up"
  | Route.AllBookListings -> "listings"
  | Route.MyBookListings -> "my-pages"
  | _ -> ""
