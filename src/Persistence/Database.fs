module Persistence.Database

open System
open System.Data
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL

module Tables =
  module Listings =
    let name = "listings"

  [<CLIMutable>]
  type Listings = {
    id: Guid
    user_id: Guid
    author: string
    title: string
    status: int
    published_date: DateTime
  }

  type Users = {
    id: Guid
    name: string
  }

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
    UserId: Guid
    Title : string
    Author : string
    Intent : int
  }

  let addListing (dbConnection: IDbConnection) (model: CreateListingModel) =
    let listingToInsert: Tables.Listings = {
       id = Guid.NewGuid()
       user_id = model.UserId
       author = model.Author
       title = model.Title
       status = 0
       published_date = DateTime.UtcNow
    }
    insert<Tables.Listings> {
      table Tables.Listings.name
      value listingToInsert
    } |> dbConnection.InsertAsync

module Queries =
  let getUserListings (dbConnection: IDbConnection) (userId: Guid) =
    select {
        table Tables.Listings.name
        where (eq "user_id" userId)
    } |> dbConnection.SelectAsync<Tables.Listings>
