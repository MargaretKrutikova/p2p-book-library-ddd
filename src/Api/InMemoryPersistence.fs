module Api.InMemoryPersistence

open System
open Core.Common.Persistence

open System.Threading.Tasks
open FsToolkit.ErrorHandling

type Persistence = {
  GetUserListings: Queries.GetUserListings
  GetUserById: Queries.GetUserById
  GetListingById: Queries.GetListingById
  CreateListing: Commands.CreateListing
  UpdateListing: Commands.UpdateListing
  CreateUser: Commands.CreateUser
}

module InMemoryPersistence =
  let create (): Persistence =
    let mutable users: Queries.UserReadModel list = List.empty
    let mutable listings: Queries.ListingReadModel list = List.empty

    let getUserListings: Queries.GetUserListings =
      fun userId ->
        listings 
        |> Seq.filter (fun listing -> listing.UserId = userId)
        |> Ok
        |> Task.FromResult 

    let getUserById: Queries.GetUserById =
      fun userId ->
        users 
        |> Seq.filter (fun user -> user.Id = userId)
        |> Seq.tryHead
        |> Result.requireSome Queries.MissingRecord
        |> Task.FromResult

    let getListingById: Queries.GetListingById =
      fun listingId ->
        listings 
        |> Seq.filter (fun listing -> listing.ListingId = listingId)
        |> Seq.tryHead
        |> Result.requireSome Queries.MissingRecord
        |> Task.FromResult

    let createListing: Commands.CreateListing =
      fun model ->
        let listing: Queries.ListingReadModel = {
          ListingId = model.ListingId 
          UserId = model.UserId
          Author = model.Author
          Title = model.Title
          Status = model.InitialStatus
          PublishedDate = DateTime.UtcNow
        }
        listings <- listing::listings
        Task.FromResult (Ok ())

    let createUser: Commands.CreateUser =
        fun userDto ->
          let user: Queries.UserReadModel = {
            Id = userDto.UserId
            Name = userDto.Name
          }
          users <- user::users
          Task.FromResult (Ok())

    let updateListing (listing: Commands.ListingUpdateModel) =
      let updatedListings = 
        listings 
          |> Seq.map (fun l -> 
            if l.ListingId = listing.ListingId then
              { l with Status = listing.Status }
            else l
          ) 
          |> Seq.toList

      listings <- updatedListings
      Task.FromResult (Ok ())

    {
      GetUserListings = getUserListings
      GetUserById = getUserById
      GetListingById = getListingById
      CreateListing = createListing
      CreateUser = createUser
      UpdateListing = updateListing
    }
