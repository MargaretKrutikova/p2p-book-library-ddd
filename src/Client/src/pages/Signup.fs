module Client.Pages.Signup

open System
open Api.Models
open Client.Utils
open Feliz
open Elmish
open Feliz.UseElmish
open Fulma
open Fable.React

type CreateUserApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | CreatedUser

type Model =
    { UserName: string
      Email: string
      CreateUserApiState: CreateUserApiState }
    static member CreateDefault() =
        { UserName = ""
          Email = ""
          CreateUserApiState = NotAsked }

let toUserInputModel model: UserRegisterInputModel =
    { Name = model.UserName
      Email = model.Email
      IsSubscribedToUserListingActivity = true }

let isSignupFormValid (model: Model): bool =
    String.IsNullOrWhiteSpace model.UserName
    |> not
    
type Msg =
    | UserNameChanged of string
    | EmailChanged of string
    | SubmitClicked
    | UserCreated of UserRegisteredOutputModel
    | UserCreateError of ApiError

let init () = Model.CreateDefault(), Cmd.none

type OnUserCreated = Guid -> unit

let update (onUserCreated: OnUserCreated) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | EmailChanged email -> { model with Email = email }, Cmd.none
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

// VIEW

let signupResultMessage (result: CreateUserApiState) =
    match result with
    | CreatedUser _ -> Notification.success "You are now registered!"
    | Error _ -> Notification.error
    | NotAsked -> div [] []
    | Loading -> div [] []

let view = React.functionComponent(fun (props: {| onUserCreated: Guid -> unit |}) ->
    let model, dispatch = React.useElmish(init, update props.onUserCreated, [| |])        
    let canSubmit =
            isSignupFormValid model
            && model.CreateUserApiState
            <> Loading

    Column.column [ Column.Width(Screen.All, Column.IsOneThird) ] [
        Heading.h2 [] [str "Sign up"]

        Box.box' [] [
            form [] [
                Field.div [] [
                    Label.label [] [ str "Username" ]
                    Control.div [] [
                        Input.text [ Input.Value model.UserName
                                     Input.OnChange(eventToInputValue >> UserNameChanged >> dispatch) ]
                    ]
                ]

                Field.div [] [
                    Control.div [] [
                        signupResultMessage model.CreateUserApiState
                    ]
                ]
                Field.div [] [
                    Control.div [] [
                        Button.button [ Button.Disabled(canSubmit |> not)
                                        Button.Color IsPrimary
                                        Button.IsFullWidth
                                        Button.OnClick(fun e ->
                                            e.preventDefault ()
                                            dispatch SubmitClicked) ] [
                            str "Sign up"
                        ]
                    ]
                ]
            ]
        ]
    ])
