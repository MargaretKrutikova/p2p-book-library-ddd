module Core.Handlers.QueryHandlers

open System
open System.Threading.Tasks
open Core.QueryModels
open FsToolkit.ErrorHandling.Operator.TaskResult
open FsToolkit.ErrorHandling

open Core.Domain.Types

module QueryPersistenceOperations =
    type DbResult<'a> = Task<Result<'a, QueryError>>
    type GetAllPublishedListings = unit -> DbResult<BookListingDto list>
    type GetListingsByOwnerId = UserId -> DbResult<UserBookListingDto seq>
    type GetUserByName = string -> DbResult<UserDto option>
    type GetListingById = ListingId -> DbResult<BookListingDto option>

type QueryPersistenceOperations =
    { GetAllPublishedListings: QueryPersistenceOperations.GetAllPublishedListings
      GetListingsByUserId: QueryPersistenceOperations.GetListingsByOwnerId
      GetUserByName: QueryPersistenceOperations.GetUserByName
      GetListingById: QueryPersistenceOperations.GetListingById }

type GetAllPublishedBookListings = unit -> QueryResult<BookListingDto list>
let getAllPublishedBookListings (getListings: QueryPersistenceOperations.GetAllPublishedListings)
                                : GetAllPublishedBookListings =
    fun () ->
        getListings ()
        |> TaskResult.mapError (fun _ -> InternalError)

type GetUserBookListings = UserId -> QueryResult<UserBookListingDto list>
let getUserBookListings (getListingsByUserId: QueryPersistenceOperations.GetListingsByOwnerId): GetUserBookListings =
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

let private toUserActivityListing (listing: BookListingDto) (status: UserActivityListingStatus): UserActivityListing =
    {  ListingId = listing.ListingId
       OwnerId = listing.OwnerId
       OwnerName = listing.OwnerName
       Author = listing.Author
       Title = listing.Title
       Status = status }

let private getUserActivityListing (userId: Guid) (listing: BookListingDto): UserActivityListing option =
    match listing.Status with
    | ListingStatusDto.Borrowed borrower when borrower.Id = userId -> BorrowedByUser |> Some
    | ListingStatusDto.RequestedToBorrow borrower when borrower.Id = userId -> RequestedByUser |> Some
    | _ -> None
    |> Option.map (toUserActivityListing listing)
type GetUserActivity = UserId -> QueryResult<UserActivity>
let getUserActivity (getAllListings: QueryPersistenceOperations.GetAllPublishedListings): GetUserActivity =
    fun userId ->
        taskResult {
            let! listings = getAllListings ()
            let activity = listings |> Seq.choose (userId |> UserId.value |> getUserActivityListing) |> Seq.toList
            return { Listings = activity }
        }
    
type QueryHandler =
    { GetAllPublishedBookListings: GetAllPublishedBookListings
      GetUserByName: GetUserByName
      GetUserBookListings: GetUserBookListings
      GetListingById: GetListingById
      GetUserActivity: GetUserActivity }

let createQueryHandler (queryPersistence: QueryPersistenceOperations): QueryHandler =
    { GetAllPublishedBookListings = getAllPublishedBookListings queryPersistence.GetAllPublishedListings
      GetUserBookListings = getUserBookListings queryPersistence.GetListingsByUserId
      GetUserByName = getUserByName queryPersistence.GetUserByName
      GetListingById = getListingById queryPersistence.GetListingById
      GetUserActivity = getUserActivity queryPersistence.GetAllPublishedListings }
