module Core.Handlers.QueryHandlers

open System.Threading.Tasks
open FsToolkit.ErrorHandling.Operator.TaskResult
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

open Core.Domain.Types

type QueryError =
   | InternalError   

type QueryResult<'a> = Task<Result<'a, QueryError>>

type BookListingDto = {
   ListingId: ListingId
   UserId: UserId
   UserName: string
   Author: string
   Title: string
   Status: ListingStatus
}

type UserBookListingDto = {
   ListingId: ListingId
   Author: string
   Title: string
   Status: ListingStatus
}

type UserDto = {
    Id: UserId
    Name: string
    Email: string
    IsSubscribedToUserListingActivity: bool
}
   
module QueryPersistenceOperations =
   type DbResult<'a> = Task<Result<'a, QueryError>>
   type GetAllListings = unit -> DbResult<BookListingDto list>
   type GetListingsByUserId = UserId -> DbResult<UserBookListingDto seq>
   type GetUserByName = string -> DbResult<UserDto option>

type QueryPersistenceOperations = {
  // GetAllListings: QueryPersistenceOperations.GetAllListings
  GetListingsByUserId: QueryPersistenceOperations.GetListingsByUserId
  GetUserByName: QueryPersistenceOperations.GetUserByName
}

type GetAllPublishedBookListings = unit -> QueryResult<BookListingDto list>
let getAllPublishedBookListings (getListings: QueryPersistenceOperations.GetAllListings): GetAllPublishedBookListings =
  fun getListings ->
    taskResult {
       return List.Empty
    }
    
type GetUserBookListings = UserId -> QueryResult<UserBookListingDto list>
let getUserBookListings (getListingsByUserId: QueryPersistenceOperations.GetListingsByUserId): GetUserBookListings =
  fun userId ->
    getListingsByUserId userId |> TaskResult.map (Seq.toList) 
    
type GetUserByName = string -> QueryResult<UserDto option>
let getUserByName (getUser: QueryPersistenceOperations.GetUserByName): GetUserByName =
   fun userName -> getUser userName |> TaskResult.mapError (fun _ -> InternalError)
