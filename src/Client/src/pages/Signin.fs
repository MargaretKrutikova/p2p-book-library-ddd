module Client.Pages.Signin

open Api.Models
open Client.Utils
open Client.Api

open Core.Domain.Errors
open Feliz
open Elmish
open Feliz.UseElmish

type LoginUserApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | LoggedInUser of UserOutputModel

type Model = {
    UserName: string
    LoginUserApiState: LoginUserApiState
}
with
    static member CreateDefault () = { UserName = ""; LoginUserApiState = NotAsked }

let toUserInputModel model: UserLoginInputModel = { Name = model.UserName }

type Msg =
    | UserNameChanged of string
    | LoginClicked
    | UserLoggedIn of UserOutputModel
    | UserLoginError of ApiError

let init () =
    Model.CreateDefault (), Cmd.none
    
type OnUserLoggedIn = UserOutputModel -> unit

let update (onUserLoggedIn: OnUserLoggedIn) (message:Msg) (model:Model) : Model * Cmd<Msg> =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | LoginClicked -> 
        let userModel = toUserInputModel model
        { model with LoginUserApiState = Loading }, Cmd.OfAsync.eitherAsResult userApi.login userModel UserLoggedIn UserLoginError
    | UserLoginError error ->
        { model with LoginUserApiState = Error error }, Cmd.none
    | UserLoggedIn user ->
        { model with LoginUserApiState = LoggedInUser user }, Cmd.ofSub (fun _ -> onUserLoggedIn user)

let view = React.functionComponent(fun (props: {| onUserLoggedIn: OnUserLoggedIn |}) ->
   let model, dispatch = React.useElmish(init, update props.onUserLoggedIn, [| |])        
   let resultView =
       match model.LoginUserApiState with
       | NotAsked -> Html.span []
       | Loading -> Html.text "..."
       | Error e -> Html.text "Error"
       | LoggedInUser _ -> Html.text "Logged in successfully!"

   Html.div [
           Html.h1 [ prop.children [ Html.text "Sign in" ] ]
           
           Html.input [
                   prop.onChange (eventToInputValue >> UserNameChanged >> dispatch)
                   prop.value model.UserName
                   prop.type' "Text"
               ]
           Html.button [ prop.onClick (fun _ -> dispatch LoginClicked); prop.children [Html.text "Sign in" ] ]
           Html.div [ resultView ]
       ]
)
