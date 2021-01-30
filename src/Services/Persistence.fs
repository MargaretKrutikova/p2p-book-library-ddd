module Services.Persistence

open System.Threading.Tasks
open Core.Domain.Types

type DbReadError = | MissingRecord | InternalError 
type DbReadResult<'a> = Task<Result<'a, DbReadError>>

type DbWriteError = | WriteError
type DbWriteResult = Task<Result<unit, DbWriteError>>

module Common =
    type GetUserById = UserId -> DbReadResult<User>
    type GetListingById = ListingId -> DbReadResult<BookListing>
    
    type CommonQueries = {
        GetUserById: GetUserById
        GetListingById: GetListingById
    }
    
module Commands =
    type CreateUser = User -> DbWriteResult
    type CreateListing = BookListing -> DbWriteResult
    type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult
    
    type CommandPersistence = {
        CreateUser: CreateUser
        CreateListing: CreateListing
        UpdateListingStatus: UpdateListingStatus
    }
