module Client.Pages.Signup

open System
open Api.Models
open Client.Utils
open Feliz
open Elmish
open Feliz.UseElmish

type CreateUserApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | CreatedUser

type Model =
    { UserName: string
      CreateUserApiState: CreateUserApiState }
    static member CreateDefault() =
        { UserName = ""
          CreateUserApiState = NotAsked }

let toUserInputModel model: UserRegisterInputModel =
    { Name = model.UserName
      Email = ""
      IsSubscribedToUserListingActivity = true }

type Msg =
    | UserNameChanged of string
    | SubmitClicked
    | UserCreated of UserRegisteredOutputModel
    | UserCreateError of ApiError

let init () = Model.CreateDefault(), Cmd.none

type OnUserCreated = Guid -> unit

let update (onUserCreated: OnUserCreated) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | SubmitClicked ->
        let userModel = toUserInputModel model
        { model with
              CreateUserApiState = Loading },
        Cmd.OfAsync.eitherAsResult Client.Api.userApi.register userModel UserCreated UserCreateError
    | UserCreateError error ->
        { model with
              CreateUserApiState = Error error },
        Cmd.none
    | UserCreated output ->
        { model with
              CreateUserApiState = CreatedUser },
        Cmd.ofSub (fun _ -> onUserCreated output.Id)

let view =
    React.functionComponent (fun (props: {| onUserCreated: Guid -> unit |}) ->
        let model, dispatch =
            React.useElmish (init, update props.onUserCreated, [||])

        let resultView =
            match model.CreateUserApiState with
            | NotAsked -> Html.span []
            | Loading -> Html.text "..."
            | Error e -> Html.text "Error"
            | CreatedUser -> Html.text "User created"

        Html.div [ Html.h1 [ prop.children [ Html.text "Sign up" ] ]

                   Html.input [ prop.onChange (eventToInputValue >> UserNameChanged >> dispatch)
                                prop.value model.UserName
                                prop.type' "Text" ]
                   Html.button [ prop.onClick (fun _ -> dispatch SubmitClicked)
                                 prop.children [ Html.text "Sign up" ] ]
                   Html.div [ resultView ] ])
