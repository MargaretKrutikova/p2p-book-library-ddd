module Client.Utils

open Api.BookListing.Models
open Fable.Core

[<Emit("Math.random()")>]
let getRandom(): float = jsNative

let eventToInputValue (event: Browser.Types.Event): string = (event.target :?> Browser.Types.HTMLInputElement).value

module Cmd =
    let exnToError (e:exn): ApiError = InternalError

    open Elmish
    
    module OfAsync =
        let eitherAsResult f args okMsg errorMsg =
           let mapResult = function
                           | Ok data -> okMsg data
                           | ApiResponse.Error error -> errorMsg error
           Cmd.OfAsync.either f args mapResult (exnToError >> errorMsg)