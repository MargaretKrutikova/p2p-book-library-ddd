module Persistence.Database

open System
open System.Data
open Core.Common.Persistence

open Dapper.FSharp
open Dapper.FSharp.PostgreSQL
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

module Tables =
  module Listings =
    let tableName = "listings"

  type ListingStatus =
    | Available = 0
    | Borrowed = 1
    | Unpublished = 2

  [<CLIMutable>]
  type Listings = {
    id: Guid
    user_id: Guid
    author: string
    title: string
    status: ListingStatus
    published_date: DateTime
  }

  module Users =
    let tableName = "users"

  [<CLIMutable>]
  type Users = {
    id: Guid
    name: string
  }

  module ListingHistory =
    let tableName = "listing_history"

  [<CLIMutable>]
  type ListingHistory = {
    id: Guid
    listing_id: Guid
    user_id: Guid
    date: DateTime
    borrower_id: Guid
    entry_type: int
  }

module Commands =
  type CreateListingModel = {
    Id: Guid
    UserId: Guid
    Title : string
    Author : string
  }

  type CreateUserModel = {
    Id: Guid
    Name: string
  }

  // TODO: should take in domain model
  let private fromCreateLisingModel (model: CreateListingModel): Tables.Listings =
    {
       id = model.Id
       user_id = model.UserId
       author = model.Author
       title = model.Title
       status = Tables.ListingStatus.Available
       published_date = DateTime.UtcNow
    }

  let createUser (dbConnection: IDbConnection) (model: CreateUserModel) =
    insert<Tables.Users> {
      table Tables.Users.tableName
      value { id = model.Id; name = model.Name }
    } |> dbConnection.InsertAsync

  let createListing (dbConnection: IDbConnection) (model: CreateListingModel) =
    insert<Tables.Listings> {
      table Tables.Listings.tableName
      value (fromCreateLisingModel model)
    } |> dbConnection.InsertAsync

module Queries =
  open Core.Common.SimpleTypes
  open Core.BookListing

  let private toListingStatus status =
    match status with
    | Tables.ListingStatus.Available -> Available
    | Tables.ListingStatus.Borrowed -> Borrowed
    | _ -> failwith "Unknown listing status"

  let private toListingReadModel (listing: Tables.Listings): Queries.ListingReadModel = {
    ListingId = ListingId.create listing.id
    UserId = UserId.create listing.user_id
    Author = listing.author
    Title = listing.title
    Status = toListingStatus listing.status 
    PublishedDate = listing.published_date
  }

  let getUserListings (dbConnection: IDbConnection) (userId: Guid) =
    select {
        table Tables.Listings.tableName
        where (eq "user_id" userId)
    } |> dbConnection.SelectAsync<Tables.Listings>

  let getListingById (dbConnection: IDbConnection): Queries.GetUserListings =
    fun listingId ->
      select {
          table Tables.Listings.tableName
          where (eq "id" listingId)
      } 
      |> dbConnection.SelectAsync<Tables.Listings>
      |> Task.map (Seq.map toListingReadModel >> Ok)
