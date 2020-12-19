module Api.BookListing.HttpHandlers

open System.Threading.Tasks
open Api.BookListing.Models
open Api.BookListing.ApiHandlers
open Api.CompositionRoot
open Core.BookListing.Service

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open Giraffe

let private errorToHttpResponse (next: HttpFunc) (ctx : HttpContext) (error: BookListingError) =
    match error with
    | ServiceError -> RequestErrors.BAD_REQUEST "" next ctx
    | UserDoesntExist -> RequestErrors.BAD_REQUEST "User doesnt exist" next ctx
    | _ -> RequestErrors.BAD_REQUEST "Unknown error" next ctx

let private toHttpResponse (next: HttpFunc) (ctx : HttpContext) (result: Result<'a, BookListingError>): Task<HttpContext option> =
    match result with
    | Ok data -> json data next ctx
    | Error error -> errorToHttpResponse next ctx error

let handleGetListings (userId: string) =
  fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
          let root = ctx.GetService<CompositionRoot>()
          let! result = getUserListings root userId
          return! result |> toHttpResponse next ctx
      }
      
let handleCreateUser (next: HttpFunc) (ctx : HttpContext) =
  task {
      let! userModel = ctx.BindJsonAsync<UserCreateInputModel>()
      let root = ctx.GetService<CompositionRoot>()
      
      let! result = createUser root userModel
      return! result |> toHttpResponse next ctx
  } 
  
let handleCreateListing (userId: string) =
  fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
          let! listingModel = ctx.BindJsonAsync<ListingCreateInputModel>()
          let root = ctx.GetService<CompositionRoot>()

          let! result = createListing root userId listingModel
          return! result |> toHttpResponse next ctx
      }