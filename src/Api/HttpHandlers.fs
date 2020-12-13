namespace Api

open Api.InMemoryPersistence
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
                let persistence = ctx.GetService<Persistence>()
                let id = Guid.Parse userId
                
                // This should be part of some other core area, like projections
                let! listings = UserId.create id |> persistence.GetUserListings 
                let response: ListingOutputModel list =
                    listings
                        |> Seq.map (fun l -> {
                            Id = ListingId.value l.ListingId
                            UserId = UserId.value l.UserId
                            Author = Author.value l.Author
                            Title = Title.value l.Title
                        })
                        |> Seq.toList
                return! json response next ctx
            }
            
    let handleCreateUser (next: HttpFunc) (ctx : HttpContext) =
        task {
            let userId = Guid.NewGuid ()
            let persistence = ctx.GetService<Persistence>()
            do! persistence.CreateUser userId "user-name"

            let response: UserCreatedOutputModel = {
                Id = userId
            }
            return! json response next ctx
        }
        
    let handleCreateListing (userId: string) =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let userId = Guid.Parse userId
                let persistence = ctx.GetService<Persistence>()

                let listingId = Guid.NewGuid ()
                let createListingCommandData: Commands.CreateBookListing =
                    {
                        BookListing = { 
                            NewListingId = Guid.NewGuid ()
                            UserId = userId
                            Title = "Test title"
                            Author= "Test author"
                        }
                        Timestamp = DateTime.Now
                    }
                let! result =
                    createListingCommandData
                    |> Commands.CreateBookListing
                    |> CommandHandler.commandHandler persistence

                match result with
                | Ok _ -> 
                    let response: ListingCreatedOutputModel = {
                        Id = listingId
                    }
                    return! json response next ctx
                | Error err ->
                    let x = err
                    return! RequestErrors.BAD_REQUEST "" next ctx
            }
