module Api.BookListing.ApiHandlers

open Api.CompositionRoot
open Api.BookListing.Models

open Core.BookListing.Domain
open Core.Common.SimpleTypes
open Core.Common.Persistence
open FsToolkit.ErrorHandling.TaskResultCE
open System

let private toListingOutputModel (listing: Queries.ListingReadModel) =
    {
        Id = ListingId.value listing.ListingId
        UserId = UserId.value listing.UserId
        Author = listing.Author
        Title = listing.Title
    }

let createUser (root: CompositionRoot) (userModel: UserCreateInputModel) =
    taskResult {
        let userId = Guid.NewGuid ()
        do! root.CreateUser { UserId = UserId.create userId; Name = userModel.Name }
        let response: UserCreatedOutputModel = { Id = userId }
        return response
  } 

let createListing (root: CompositionRoot) (userIdStr: string) (listingModel: ListingCreateInputModel) =
  taskResult {
      let userId = Guid.Parse userIdStr

      let listingId = Guid.NewGuid ()
      let listingToCreate: CreateBookListingDto = {
          NewListingId = ListingId.create listingId
          UserId = UserId.create userId
          Title = listingModel.Title
          Author = listingModel.Author
      }
      
      do! root.CreateListing listingToCreate
      let response: ListingCreatedOutputModel = {
          Id = listingId
      }
      return response
  }

let getUserListings (root: CompositionRoot) (userIdStr: string) =
      taskResult {
        let userId = userIdStr |> Guid.Parse |> UserId.create
        
        let! listings = root.GetUserListings userId 
        return listings
            |> Seq.map toListingOutputModel
            |> Seq.toList
      }