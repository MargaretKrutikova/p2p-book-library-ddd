module Client.Pages.Signin

open Api.Models
open Client.Utils
open Client.Api

open Fulma
open Feliz
open Elmish
open Feliz.UseElmish
open Fable.React

type LoginUserApiState =
    | NotAsked
    | Loading
    | Error of ApiError
    | LoggedInUser of UserOutputModel

type Model =
    { UserName: string
      LoginUserApiState: LoginUserApiState }
    static member CreateDefault() =
        { UserName = ""
          LoginUserApiState = NotAsked }

let toUserInputModel model: UserLoginInputModel = { Name = model.UserName }

let isSigninFormValid (model: Model): bool =
    System.String.IsNullOrWhiteSpace model.UserName
    |> not

type Msg =
    | UserNameChanged of string
    | LoginClicked
    | UserLoggedIn of UserOutputModel
    | UserLoginError of ApiError

let init () = Model.CreateDefault(), Cmd.none

type OnUserLoggedIn = UserOutputModel -> unit

let update (onUserLoggedIn: OnUserLoggedIn) (message: Msg) (model: Model): Model * Cmd<Msg> =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | LoginClicked ->
        let userModel = toUserInputModel model
        { model with
              LoginUserApiState = Loading },
        Cmd.OfAsync.eitherAsResult userApi.login userModel UserLoggedIn UserLoginError
    | UserLoginError error ->
        { model with
              LoginUserApiState = Error error },
        Cmd.none
    | UserLoggedIn user ->
        { model with
              LoginUserApiState = LoggedInUser user },
        Cmd.ofSub (fun _ -> onUserLoggedIn user)


// VIEW

let signinResultMessage (loginResult: LoginUserApiState) =
    match loginResult with
    | LoggedInUser _ ->
        Notification.notification [ Notification.Color IsSuccess
                                    Notification.IsLight ] [
            str "Logged in successfully!"
        ]
    | Error _ ->
        Notification.notification [ Notification.Color IsDanger
                                    Notification.IsLight ] [
            str "An unexpected error occurred. Please try again later."
        ]
    | NotAsked -> div [] []
    | Loading -> div [] []

let view =
    React.functionComponent (fun (props: {| onUserLoggedIn: OnUserLoggedIn |}) ->
        let model, dispatch =
            React.useElmish (init, update props.onUserLoggedIn, [||])

        let canSubmit =
            isSigninFormValid model
            && model.LoginUserApiState
            <> Loading

        Column.column [ Column.Width(Screen.All, Column.IsOneThird) ] [
            Heading.h2 [] [str "Log in"]

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
                            signinResultMessage model.LoginUserApiState
                        ]
                    ]
                    Field.div [] [
                        Control.div [] [
                            Button.button [ Button.Disabled(canSubmit |> not)
                                            Button.Color IsPrimary
                                            Button.IsFullWidth
                                            Button.OnClick(fun e ->
                                                e.preventDefault ()
                                                dispatch LoginClicked) ] [
                                str "Log in"
                            ]
                        ]
                    ]
                ]
            ]
        ])
