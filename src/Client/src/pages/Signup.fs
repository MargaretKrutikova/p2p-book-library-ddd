module Client.Pages.Signup

open Api.BookListing.Models
open Client.Utils
open Feliz
open Elmish
open Feliz.UseElmish

type CreateUserApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | CreatedUser

type Model = {
    UserName: string
    CreateUserApiState: CreateUserApiState
}
with
    static member CreateDefault () = { UserName = ""; CreateUserApiState = NotAsked }

let toUserInputModel model: UserCreateInputModel = { Name = model.UserName }

type Msg =
    | UserNameChanged of string
    | SubmitClicked
    | UserCreated of UserCreatedOutputModel
    | UserCreateError of ApiError

let init () =
    Model.CreateDefault (), Cmd.none
    
let update (message:Msg) (model:Model) : Model * Cmd<Msg> =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | SubmitClicked -> 
        let userModel = toUserInputModel model
        { model with CreateUserApiState = Loading }, Cmd.OfAsync.eitherAsResult Client.Api.userApi.create userModel UserCreated UserCreateError
    | UserCreateError error ->
        { model with CreateUserApiState = Error error }, Cmd.none
    | UserCreated output ->
        // TODO: redirect on success
        { model with CreateUserApiState = CreatedUser }, Cmd.none
let view = React.functionComponent(fun () ->
   let model, dispatch = React.useElmish(init, update, [| |])        
   let resultView =
       match model.CreateUserApiState with
       | NotAsked -> Html.span []
       | Loading -> Html.text "..."
       | Error e -> Html.text "Error"
       | CreatedUser -> Html.text "User created"

   Html.div [
           Html.h1 [ prop.children [ Html.text "Sign up" ] ]
           
           Html.input [
                   prop.onChange (eventToInputValue >> UserNameChanged >> dispatch)
                   prop.value model.UserName
                   prop.type' "Text"
               ]
           Html.button [ prop.onClick (fun _ -> dispatch SubmitClicked); prop.children [Html.text "Sign up" ] ]
           Html.div [ resultView ]
       ]
)