module InMemoryPersistence

open System
open BookListing
open System.Threading.Tasks
open FsToolkit.ErrorHandling

type InMemoryPersistence () =
  let mutable users: Persistence.Queries.User list = List.empty
  let mutable listings: Persistence.Queries.Listing list = List.empty

  member __.GetUserListings: Persistence.Queries.GetUserListings =
    fun userId ->
      listings 
      |> Seq.filter (fun listing -> listing.UserId = userId)
      |> Task.FromResult

  member __.GetUserById: Persistence.Queries.GetUserById =
    fun userId ->
      users 
      |> Seq.filter (fun user -> user.Id = userId)
      |> Seq.tryHead
      |> Result.requireSome (Persistence.Queries.DbReadError.MissingRecord)
      |> Task.FromResult

  member __.GetListingById: Persistence.Queries.GetListingById =
    fun listingId ->
      listings 
      |> Seq.filter (fun listing -> listing.ListingId = listingId)
      |> Seq.tryHead
      |> Result.requireSome (Persistence.Queries.DbReadError.MissingRecord)
      |> Task.FromResult

  member __.CreateListing: Persistence.Commands.CreateListing =
    fun model ->
      let listing: Persistence.Queries.Listing = {
        ListingId = model.ListingId 
        UserId = model.UserId
        Author = model.Author
        Title = model.Title
        Status = model.InitialStatus
        PublishedDate = DateTime.UtcNow
      }
      listings <- listing::listings
      Task.FromResult ()
