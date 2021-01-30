module Persistence.Database
open Core.Domain.Types

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
      | ListingStatus.Available -> Tables.ListingStatus.Available
      | ListingStatus.Borrowed _ -> Tables.ListingStatus.Borrowed
      | ListingStatus.RequestedToBorrow _ -> Tables.ListingStatus.RequestedToBorrow
    
    let fromDbListingStatus (status: Tables.ListingStatus): ListingStatus =
      match status with
      | Tables.ListingStatus.Available -> ListingStatus.Available
      | Tables.ListingStatus.Borrowed -> ListingStatus.Borrowed dummyUserId
      | Tables.ListingStatus.RequestedToBorrow -> ListingStatus.RequestedToBorrow dummyUserId
      | _ -> failwith "Unknown listing status"
    
    let toDbListing (listing: BookListing): Tables.Listings =
      {
         id = listing.Id |> ListingId.value
         user_id = listing.OwnerId |> UserId.value
         author = listing.Author
         title = listing.Title
         status = toDbListingStatus listing.Status
         published_date = DateTime.UtcNow // TODO: pass in somehow
      }
      
    let toDbUser (user: User): Tables.Users =
      {
        id = user.UserId |> UserId.value
        name = user.Name
      }
    
    let toUserReadModel (dbUser: Tables.Users): User =
      {
        UserId = UserId.create dbUser.id
        Name = dbUser.name
        Email = ""
        UserSettings = {
          IsSubscribedToUserListingActivity = true
        }
      }
    
module CommandPersistenceImpl =
  open Services.Persistence
  
  let createUser (dbConnection: IDbConnection): Commands.CreateUser =
    fun user ->
      insert<Tables.Users> {
        table Tables.Users.tableName
        value (Conversions.toDbUser user)
      }
      |> dbConnection.InsertAsync
      |> Task.map (fun _ -> Ok ()) // TODO: proper error handling

  let createListing (dbConnection: IDbConnection): Commands.CreateListing =
    fun model ->
      insert<Tables.Listings> {
        table Tables.Listings.tableName
        value (Conversions.toDbListing model)
      }
      |> dbConnection.InsertAsync
      |> Task.map (fun _ -> Ok ()) // TODO: proper error handling
  
  let getUserById (dbConnection: IDbConnection): Common.GetUserById =
    fun userId ->
        select {
            table Tables.Users.tableName
            where (eq "id" (userId |> UserId.value))
        } 
        |> dbConnection.SelectAsync<Tables.Users>
        |> Task.map (Seq.head >> Conversions.toUserReadModel >> Ok) // TODO: handle missing user
        
module QueryPersistenceImpl =
  open Services.QueryHandlers
  open Services.QueryModels
    
  let private toUserBookListingDto (listing: Tables.Listings): UserBookListingDto = {
    ListingId = listing.id
    Author = listing.author
    Title = listing.title
    Status = failwith "" 
  }

  let private toUserDto (dbUser: Tables.Users): UserDto =
      {
        Id = dbUser.id
        Name = dbUser.name
        Email = "" // TODO: add to db
        IsSubscribedToUserListingActivity = true 
      }
  
  let getListingsByUserId (dbConnection: IDbConnection): QueryPersistenceOperations.GetListingsByOwnerId =
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
