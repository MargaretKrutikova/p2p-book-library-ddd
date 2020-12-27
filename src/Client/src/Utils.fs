module Client.Utils

open System
open Api.Models
open Fable.Core

[<Emit("Math.random()")>]
let getRandom(): float = jsNative

let eventToInputValue (event: Browser.Types.Event): string = (event.target :?> Browser.Types.HTMLInputElement).value

let stringNotEmpty str = String.IsNullOrWhiteSpace str

module Cmd =
    let exnToError (e:exn): ApiError = ApiError.InternalError

    open Elmish
    
    module OfAsync =
        let eitherAsResult f args okMsg errorMsg =
           let mapResult = function
                           | Ok data -> okMsg data
                           | ApiResponse.Error error -> errorMsg error
           Cmd.OfAsync.either f args mapResult (exnToError >> errorMsg)
