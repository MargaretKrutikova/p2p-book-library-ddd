module Client.Api

open Fable.Remoting.Client
open Api.Models

let userApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder IUserApi.RouteBuilder
  |> Remoting.buildProxy<IUserApi>

let bookListingApi =
  Remoting.createApi()
  |> Remoting.withRouteBuilder IBookListingApi.RouteBuilder
  |> Remoting.buildProxy<IBookListingApi>
