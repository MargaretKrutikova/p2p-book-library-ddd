module Core.Handlers.QueryHandlers

open System.Threading.Tasks
open Core.Common.SimpleTypes
open Core.Domain
open FsToolkit.ErrorHandling.TaskResultCE

type QueryResult<'a> = Task<Result<'a, Errors.AppError>>
type QueryHandler<'a> = Messages.Query -> QueryResult<'a>

type BookListingDto = {
   ListingId: ListingId
   UserId: UserId
   UserName: string
   Author: string
   Title: string
   Status: ListingStatus
}
   
module QueryPersistenceOperations =
   type DbQueryError =
       | ConnectionError
       
   type DbResult<'a> = Task<Result<'a, DbQueryError>>
   type GetAllListings = unit -> DbResult<BookListingDto list> 

type QueryPersistenceOperations = {
  GetAllListings: QueryPersistenceOperations.GetAllListings
}

type GetAllPublishedBookListings = unit -> QueryResult<BookListingDto list>
let getAllPublishedBookListings (getListings: QueryPersistenceOperations.GetAllListings): GetAllPublishedBookListings =
  fun getListings ->
    taskResult {
       return List.Empty
    }