namespace Api

open Core.BookListing.Domain
open System

module HttpHandlers =
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks
  open Giraffe
  open Api.Models
  open Core.Common.SimpleTypes

  let handleGetListings (userId: string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let id = Guid.Parse userId
        
        let root = ctx.GetService<CompositionRoot.CompositionRoot>()
        
        match! UserId.create id |> root.GetUserListings with
        | Ok listings ->
          let response: ListingOutputModel list =
            listings
              |> Seq.map (fun l -> {
                  Id = ListingId.value l.ListingId
                  UserId = UserId.value l.UserId
                  Author = l.Author
                  Title = l.Title
              })
              |> Seq.toList
          return! json response next ctx
        | Error _ -> return! RequestErrors.BAD_REQUEST "" next ctx
      }
        
  let handleCreateUser (next: HttpFunc) (ctx : HttpContext) =
    task {
        let userId = Guid.NewGuid ()

        let root = ctx.GetService<CompositionRoot.CompositionRoot>()
        match! root.CreateUser { UserId = UserId.create userId; Name = "user-name" } with
        | Ok () -> 
            let response: UserCreatedOutputModel = {
                Id = userId
            }
            return! json response next ctx
        | Error _ -> return! RequestErrors.BAD_REQUEST "" next ctx
    }
    
  let handleCreateListing (userId: string) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let userId = Guid.Parse userId
            let root = ctx.GetService<CompositionRoot.CompositionRoot>()

            let listingId = Guid.NewGuid ()
            let listingToCreate: CreateBookListingDto = {
                NewListingId = ListingId.create listingId
                UserId = UserId.create userId
                Title = "title"
                Author = "author"
            }
            let! result = root.CreateListing listingToCreate

            match result with
            | Ok _ -> 
                let response: ListingCreatedOutputModel = {
                    Id = listingId
                }
                return! json response next ctx
            | Error _ ->
                return! RequestErrors.BAD_REQUEST "" next ctx
        }
