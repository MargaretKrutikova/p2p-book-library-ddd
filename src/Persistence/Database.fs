module Persistence.Database
open Core.Domain.Types
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

open System
open System.Data
open FsToolkit.ErrorHandling

open Dapper.FSharp
open Dapper.FSharp.PostgreSQL

module Tables =
  module Listings =
    let tableName = "listings"

  type ListingStatus =
    | Available = 0
    | Borrowed = 1
    | RequestedToBorrow = 2

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

module Conversions =
    let dummyUserId = failwith "implement borrow requests with user ids of borrowers"
    let toDbListingStatus (status: ListingStatus): Tables.ListingStatus =
      match status with
      | Available -> Tables.ListingStatus.Available
      | Borrowed _ -> Tables.ListingStatus.Borrowed
      | RequestedToBorrow _ -> Tables.ListingStatus.RequestedToBorrow
    
    let fromDbListingStatus (status: Tables.ListingStatus): ListingStatus =
      match status with
      | Tables.ListingStatus.Available -> Available
      | Tables.ListingStatus.Borrowed -> Borrowed dummyUserId
      | Tables.ListingStatus.RequestedToBorrow -> RequestedToBorrow dummyUserId
      | _ -> failwith "Unknown listing status"
    
    let toDbListing (listing: BookListing): Tables.Listings =
      {
         id = listing.ListingId |> ListingId.value
         user_id = listing.UserId |> UserId.value
         author = listing.Author |> Author.value
         title = listing.Title |> Title.value
         status = toDbListingStatus listing.Status
         published_date = DateTime.UtcNow // TODO: pass in somehow
      }
      
    let toDbUser (user: User): Tables.Users =
      {
        id = user.UserId |> UserId.value
        name = user.Name
      }
    
    let toUserReadModel (dbUser: Tables.Users): CommandPersistenceOperations.UserReadModel =
      {
        Id = UserId.create dbUser.id
        Name = dbUser.name
      }
    
module CommandPersistenceImpl =
  open CommandPersistenceOperations

  let createUser (dbConnection: IDbConnection): CreateUser =
    fun user ->
      insert<Tables.Users> {
        table Tables.Users.tableName
        value (Conversions.toDbUser user)
      }
      |> dbConnection.InsertAsync
      |> Task.map (fun _ -> Ok ()) // TODO: proper error handling

  let createListing (dbConnection: IDbConnection): CreateListing =
    fun model ->
      insert<Tables.Listings> {
        table Tables.Listings.tableName
        value (Conversions.toDbListing model)
      }
      |> dbConnection.InsertAsync
      |> Task.map (fun _ -> Ok ()) // TODO: proper error handling
  
  let getUserById (dbConnection: IDbConnection): GetUserById =
    fun userId ->
        select {
            table Tables.Users.tableName
            where (eq "id" (userId |> UserId.value))
        } 
        |> dbConnection.SelectAsync<Tables.Users>
        |> Task.map (Seq.head >> Conversions.toUserReadModel >> Ok) // TODO: handle missing user
        
module QueryPersistenceImpl =
  open QueryPersistenceOperations
  
  let private toUserBookListingDto (listing: Tables.Listings): UserBookListingDto = {
    ListingId = ListingId.create listing.id
    Author = listing.author
    Title = listing.title
    Status = Conversions.fromDbListingStatus listing.status 
  }

  let private toUserDto (dbUser: Tables.Users): UserDto =
      {
        Id = UserId.create dbUser.id
        Name = dbUser.name
      }
  
  let getListingsByUserId (dbConnection: IDbConnection): GetListingsByUserId =
    fun userId ->
      select {
          table Tables.Listings.tableName
          where (eq "user_id" (userId |> UserId.value))
      }
      |> dbConnection.SelectAsync<Tables.Listings>
      |> Task.map (Seq.map toUserBookListingDto >> Ok)

  let getUserByName (dbConnection: IDbConnection): GetUserByName =
    fun userName ->
      select {
          table Tables.Users.tableName
          where (eq "name" userName)
      } 
      |> dbConnection.SelectAsync<Tables.Users>
      |> Task.map (Seq.tryHead >> Option.map toUserDto >> Ok)
