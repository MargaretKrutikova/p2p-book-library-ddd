module Client.Utils

open System
open Api.Models
open Fable.Core

[<Emit("Math.random()")>]
let getRandom (): float = jsNative

let eventToInputValue (event: Browser.Types.Event): string =
    (event.target :?> Browser.Types.HTMLInputElement).value

let stringNotEmpty str = String.IsNullOrWhiteSpace str |> not

module Cmd =
    let exnToError (e: exn): ApiError = ApiError.InternalError

    open Elmish

    module OfAsync =
        let eitherAsResult f args okMsg errorMsg =
            let mapResult =
                function
                | Ok data -> okMsg data
                | ApiResponse.Error error -> errorMsg error

            Cmd.OfAsync.either f args mapResult (exnToError >> errorMsg)

module Notification =
    open Fulma
    open Fable.React

    let error =
        Notification.notification
            [ Notification.Color IsDanger; Notification.IsLight ]
            [ str "An unexpected error occurred. Please try again later." ]

    let success text =
        Notification.notification
            [ Notification.Color IsSuccess; Notification.IsLight ]
            [ str text ]

module User =
    open Browser.WebStorage
    let private loggedInUserNameStorageKey = "p2p-library/logged-in-user-name"
    type UserName = string
    let getLoggedInUserNameFromStorage (): UserName option =
      let userName = localStorage.getItem(loggedInUserNameStorageKey)
      if userName |> String.IsNullOrWhiteSpace then
        None
      else 
        Some userName
        
    let saveLoggedInUserNameInStorage (userName: UserName) =
      localStorage.setItem(loggedInUserNameStorageKey, userName)
