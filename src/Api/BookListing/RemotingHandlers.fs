module Api.BookListing.RemotingHandlers

open Api.BookListing.Models
open Api.BookListing.ApiHandlers
open Api.CompositionRoot
open Core.BookListing.Service

open Microsoft.AspNetCore.Http
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling
open System

let private toApiError (error: BookListingError): ApiError =
    match error with
    | UserDoesntExist -> UserNotFound
    | ListingDoesntExist -> ListingNotFound
    | ServiceError -> InternalError
    | _ -> failwith "Unknown error"

let private failOnExn (res: Result<'a, exn>) =
    match res with
    | Ok value -> value
    | Error ex -> raise ex

let private taskToApiResult task = task |> (AsyncResult.ofTask >> Async.map (failOnExn >> Result.mapError toApiError))

let getById (id: Guid): Async<Result<UserCreatedOutputModel, ApiError>> =
    async {
        return Ok { Id = id }
    }

let private createUserApiFromContext (ctx:HttpContext): IUserApi = 
    let root = ctx.GetService<CompositionRoot>()
    { 
        create = createUser root >> taskToApiResult
        getById = getById
    }

let private createBookListingApiFromContext (ctx: HttpContext): IBookListingApi =
    let root = ctx.GetService<CompositionRoot>()
    {
        getByUserId = getUserListings root >> taskToApiResult
        create = createListing root >> taskToApiResult
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
