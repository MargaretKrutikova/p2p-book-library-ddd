module Api.InMemoryPersistence

open System
open Core.BookListing
open Core.Common.SimpleTypes

open System.Threading.Tasks
open FsToolkit.ErrorHandling

type Persistence = {
  GetUserListings: Persistence.Queries.GetUserListings
  GetUserById: Persistence.Queries.GetUserById
  GetListingById: Persistence.Queries.GetListingById
  CreateListing: Persistence.Commands.CreateListing
  CreateUser: Guid -> string -> Task<unit>
}

module InMemoryPersistence =
  open Persistence

  let create (): Persistence =
    let mutable users: Queries.User list = List.empty
    let mutable listings: Queries.Listing list = List.empty

    let getUserListings: Queries.GetUserListings =
      fun userId ->
        listings 
        |> Seq.filter (fun listing -> listing.UserId = userId)
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
        let listing: Queries.Listing = {
          ListingId = model.ListingId 
          UserId = model.UserId
          Author = model.Author
          Title = model.Title
          Status = model.InitialStatus
          PublishedDate = DateTime.UtcNow
        }
        listings <- listing::listings
        Task.FromResult ()

    let createUser (id: Guid) (name: string) =
        let user: Queries.User = {
          Id = UserId.create id
          Name = name
        }
        users <- user::users
        Task.FromResult ()

    {
      GetUserListings = getUserListings
      GetUserById = getUserById
      GetListingById = getListingById
      CreateListing = createListing
      CreateUser = createUser
    }
