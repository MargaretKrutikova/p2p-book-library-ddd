module Api.RemotingHandlers

open Api.Models
open Api.ApiHandlers
open Api.CompositionRoot

open Microsoft.AspNetCore.Http
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling

let private failOnExn (res: Result<'a, exn>) =
    match res with
    | Ok value -> value
    | Error ex -> raise ex

let private taskToApiResult task =
    task |> (AsyncResult.ofTask >> Async.map failOnExn)

let private createUserApiFromContext (ctx:HttpContext): IUserApi = 
    let root = ctx.GetService<CompositionRoot>()
    { 
        register = registerUser root >> taskToApiResult
        login = loginUser root >> taskToApiResult
    }

let private createBookListingApiFromContext (ctx: HttpContext): IBookListingApi =
    let root = ctx.GetService<CompositionRoot>()
    {
        getByUserId = getUserListings root >> taskToApiResult
        publish = publishListing root >> taskToApiResult
        getAllListings = getAllPublishedListings root >> taskToApiResult
    }

let createUserApiHandler () : HttpHandler = 
    Remoting.createApi()
    |> Remoting.withRouteBuilder IUserApi.RouteBuilder
    |> Remoting.fromContext createUserApiFromContext
    |> Remoting.buildHttpHandler 

let createBookListingApiHandler () : HttpHandler = 
    Remoting.createApi()
    |> Remoting.withRouteBuilder IBookListingApi.RouteBuilder
    |> Remoting.fromContext createBookListingApiFromContext
    |> Remoting.buildHttpHandler 
