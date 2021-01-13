module Core.Handlers.QueryHandlers

open System.Threading.Tasks
open Core.QueryModels
open FsToolkit.ErrorHandling.Operator.TaskResult
open FsToolkit.ErrorHandling

open Core.Domain.Types

module QueryPersistenceOperations =
    type DbResult<'a> = Task<Result<'a, QueryError>>
    type GetAllPublishedListings = unit -> DbResult<BookListingDto list>
    type GetListingsByUserId = UserId -> DbResult<UserBookListingDto seq>
    type GetUserByName = string -> DbResult<UserDto option>
    type GetListingById = ListingId -> DbResult<BookListingDto option>

type QueryPersistenceOperations =
    { GetAllPublishedListings: QueryPersistenceOperations.GetAllPublishedListings
      GetListingsByUserId: QueryPersistenceOperations.GetListingsByUserId
      GetUserByName: QueryPersistenceOperations.GetUserByName
      GetListingById: QueryPersistenceOperations.GetListingById }

type GetAllPublishedBookListings = unit -> QueryResult<BookListingDto list>
let getAllPublishedBookListings (getListings: QueryPersistenceOperations.GetAllPublishedListings)
                                : GetAllPublishedBookListings =
    fun () ->
        getListings ()
        |> TaskResult.mapError (fun _ -> InternalError)

type GetUserBookListings = UserId -> QueryResult<UserBookListingDto list>
let getUserBookListings (getListingsByUserId: QueryPersistenceOperations.GetListingsByUserId): GetUserBookListings =
    fun userId ->
        getListingsByUserId userId
        |> TaskResult.map (Seq.toList)

type GetUserByName = string -> QueryResult<UserDto option>
let getUserByName (getUser: QueryPersistenceOperations.GetUserByName): GetUserByName =
    fun userName ->
        getUser userName
        |> TaskResult.mapError (fun _ -> InternalError)

type GetListingById = ListingId -> QueryResult<BookListingDto option>
let getListingById (getListingById: QueryPersistenceOperations.GetListingById): GetListingById =
    fun listingId ->
        getListingById listingId
        |> TaskResult.mapError (fun _ -> InternalError)

type QueryHandler =
    { GetAllPublishedBookListings: GetAllPublishedBookListings
      GetUserByName: GetUserByName
      GetUserBookListings: GetUserBookListings
      GetListingById: GetListingById }

let createQueryHandler (queryPersistence: QueryPersistenceOperations): QueryHandler =
    { GetAllPublishedBookListings = getAllPublishedBookListings queryPersistence.GetAllPublishedListings
      GetUserBookListings = getUserBookListings queryPersistence.GetListingsByUserId
      GetUserByName = getUserByName queryPersistence.GetUserByName
      GetListingById = getListingById queryPersistence.GetListingById }
