module Api.BookListing.RemotingHandlers

open Api.BookListing.Models
open Api.BookListing.ApiHandlers
open Api.CompositionRoot
open Core.BookListing.Service

open Core.Users.Service
open Microsoft.AspNetCore.Http
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling

let private fromBookListingError (error: BookListingError): ApiError =
    match error with
    | UserDoesntExist -> UserNotFound
    | ListingDoesntExist -> ListingNotFound
    | BookListingError.ServiceError -> InternalError
    | _ -> failwith "Unknown error"

let private fromUserError (error: UserError): ApiError =
    match error with
    | UserWithProvidedNameNotFound -> UserNotFound
    | UserError.ServiceError -> InternalError

let private failOnExn (res: Result<'a, exn>) =
    match res with
    | Ok value -> value
    | Error ex -> raise ex

let private taskToApiResult toApiError task =
    task |> (AsyncResult.ofTask >> Async.map (failOnExn >> Result.mapError toApiError))

let private createUserApiFromContext (ctx:HttpContext): IUserApi = 
    let root = ctx.GetService<CompositionRoot>()
    { 
        create = createUser root >> taskToApiResult fromUserError
        login = loginUser root >> taskToApiResult fromUserError
    }

let private createBookListingApiFromContext (ctx: HttpContext): IBookListingApi =
    let root = ctx.GetService<CompositionRoot>()
    {
        getByUserId = getUserListings root >> taskToApiResult fromBookListingError
        create = createListing root >> taskToApiResult fromBookListingError
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
