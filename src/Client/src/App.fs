module App

open Api.BookListing.Models

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

let eventToInputValue (event: Browser.Types.Event): string = (event.target :?> Browser.Types.HTMLInputElement).value

let toUserInputModel model: UserCreateInputModel = { Name = model.UserName }

type Msg =
    | UserNameChanged of string
    | SubmitClicked
    | UserCreated of UserCreatedOutputModel
    | UserCreateError of ApiError

open Fable.Core
open Fable.Remoting.Client

let userApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder IUserApi.RouteBuilder
  |> Remoting.buildProxy<IUserApi>

[<Emit("Math.random()")>]
let getRandom(): float = jsNative

module Cmd =
    let exnToError (e:exn): ApiError = InternalError

    open Elmish
    
    module OfAsync =
        let eitherAsResult f args okMsg errorMsg =
           let mapResult = function
                           | Ok data -> okMsg data
                           | ApiResponse.Error error -> errorMsg error
           Cmd.OfAsync.either f args mapResult (exnToError >> errorMsg)
    
open Elmish

let init () =
    Model.CreateDefault (), Cmd.none
    
let update message model =
    match message with
    | UserNameChanged name -> { model with UserName = name }, Cmd.none
    | SubmitClicked -> 
        let userModel = toUserInputModel model
        { model with CreateUserApiState = Loading }, Cmd.OfAsync.eitherAsResult userApi.create userModel UserCreated UserCreateError
    | UserCreateError error ->
        { model with CreateUserApiState = Error error }, Cmd.none
    | UserCreated output ->
        // TODO: redirect on success
        { model with CreateUserApiState = CreatedUser }, Cmd.none

open Fable.React
open Fable.React.Props
        
let view (model: Model) dispatch =
   let resultView =
       match model.CreateUserApiState with
       | NotAsked -> span [] []
       | Loading -> span [] [ str "..." ]
       | Error e -> span [] [ str "Error" ]
       | CreatedUser -> span [] [ str "User created" ]

   div [] [
       h1 [] [ str "Sign up" ]
       
       input [
               OnChange (eventToInputValue >> UserNameChanged >> dispatch)
               Type "text"
               Value model.UserName
           ]
       button [ OnClick (fun _ -> dispatch SubmitClicked) ] [ str "Sign up" ]
       div [] [ resultView ]
   ]
   
open Elmish.React
        
Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
|> Program.run
