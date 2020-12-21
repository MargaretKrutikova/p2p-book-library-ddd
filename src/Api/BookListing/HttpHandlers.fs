module Api.BookListing.HttpHandlers

open System.Threading.Tasks
open Api.BookListing.Models
open Api.BookListing.ApiHandlers
open Api.CompositionRoot
open Core.BookListing.Service

open Core.Users.Service
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open Giraffe
open System

let private errorToHttpResponse (next: HttpFunc) (ctx : HttpContext) (error: BookListingError) =
    match error with
    | BookListingError.ServiceError -> RequestErrors.BAD_REQUEST "" next ctx
    | UserDoesntExist -> RequestErrors.BAD_REQUEST "User doesnt exist" next ctx
    | _ -> RequestErrors.BAD_REQUEST "Unknown error" next ctx

let private userErrorToHttpResponse (next: HttpFunc) (ctx : HttpContext) (error: UserError) =
    match error with
    | UserError.ServiceError -> RequestErrors.BAD_REQUEST "" next ctx
    | UserError.UserWithProvidedNameNotFound -> RequestErrors.BAD_REQUEST "User doesnt exist" next ctx

let private toHttpResponse (next: HttpFunc) (ctx : HttpContext) toApiError (result: Result<'a, 'b>): Task<HttpContext option> =
    match result with
    | Ok data -> json data next ctx
    | Error error -> toApiError next ctx error

let handleGetListings (userId: Guid) =
  fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
          let root = ctx.GetService<CompositionRoot>()
          let! result = getUserListings root userId
          return! result |> toHttpResponse next ctx errorToHttpResponse
      }
      
let handleCreateUser (next: HttpFunc) (ctx : HttpContext) =
  task {
      let! userModel = ctx.BindJsonAsync<UserCreateInputModel>()
      let root = ctx.GetService<CompositionRoot>()
      
      let! result = createUser root userModel
      return! result |> toHttpResponse next ctx userErrorToHttpResponse
  } 
  
let handleCreateListing () =
  fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
          let! listingModel = ctx.BindJsonAsync<ListingCreateInputModel>()
          let root = ctx.GetService<CompositionRoot>()

          let! result = createListing root listingModel
          return! result |> toHttpResponse next ctx errorToHttpResponse
      }
