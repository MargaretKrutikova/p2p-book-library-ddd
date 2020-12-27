module Api.BookListing.ApiHandlers

open Api.CompositionRoot
open Api.BookListing.Models

open Core.Common.SimpleTypes
open Core.Common.Persistence
open Core.Domain
open FsToolkit.ErrorHandling.TaskResultCE
open System

let private toListingOutputModel (listing: Queries.ListingReadModel) =
    {
        Id = ListingId.value listing.ListingId
        UserId = UserId.value listing.UserId
        Author = listing.Author
        Title = listing.Title
    }

let private toPublishBookListingArgs (listingId: Guid) (listingModel: ListingCreateInputModel): Messages.PublishBookListingArgs = {
    NewListingId = ListingId.create listingId
    UserId = UserId.create listingModel.UserId 
    Title = listingModel.Title
    Author = listingModel.Author
}

let private toRegisterUserArgs (userId: Guid) (inputModel: UserCreateInputModel): Messages.RegisterUserArgs = {
    UserId = UserId.create userId
    Name = inputModel.Name
}

let createUser (root: CompositionRoot) (userModel: UserCreateInputModel) =
    taskResult {
        let userId = Guid.NewGuid ()
        let command = toRegisterUserArgs userId userModel |> Messages.RegisterUser
        do! root.CommandHandler command
        
        let response: UserCreatedOutputModel = { Id = userId }
        return response
  }

let createListing (root: CompositionRoot) (listingModel: ListingCreateInputModel) =
  taskResult {
      let listingId = Guid.NewGuid ()
      let command = toPublishBookListingArgs listingId listingModel |> Messages.PublishBookListing  
      do! root.CommandHandler command

      let response: ListingCreatedOutputModel = { Id = listingId }
      return response
  }

let loginUser (root: CompositionRoot) (userModel: UserLoginInputModel) =
    taskResult {
        let! user = root.GetUserByName userModel.Name
        let response: UserOutputModel = { UserId = user.Id |> UserId.value; Name = user.Name }
        return response
  } 

let getUserListings (root: CompositionRoot) (userId: Guid) =
      taskResult {
        let! listings = UserId.create userId |> root.GetUserListings 
        return listings
            |> Seq.map toListingOutputModel
            |> Seq.toList
      }
