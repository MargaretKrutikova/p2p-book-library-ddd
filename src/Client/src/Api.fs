module Client.Api

open Fable.Remoting.Client
open Api.Models

let userApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder IUserApi.RouteBuilder
  |> Remoting.buildProxy<IUserApi>

let listingApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder IBookListingApi.RouteBuilder
  |> Remoting.buildProxy<IBookListingApi>

type ApiState<'a> =
    | NotAsked
    | Loading
    | Error of ApiError
    | Data of 'a
    
let updateApiState (update: 'a -> 'a) (state: ApiState<'a>) =
    match state with
    | ApiState.Data data -> update data |> Data
    | other -> other
