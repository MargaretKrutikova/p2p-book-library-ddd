module Core.Common.Persistence

open Core.Common.SimpleTypes

open System
open System.Threading.Tasks

module Queries =
  type DbReadError =
    | MissingRecord

  type DbResult<'a> = Task<Result<'a, DbReadError>>

  type ListingReadModel = {
    ListingId: ListingId
    UserId: UserId
    Author: String
    Title: String
    Status: ListingStatus
    PublishedDate: DateTime
  }

  type UserReadModel = {
    Id: UserId
    Name: string
  }

  type GetListingById = ListingId -> DbResult<ListingReadModel>
  type GetUserListings = UserId -> DbResult<ListingReadModel seq>
  type GetUserById = UserId -> DbResult<UserReadModel>
  type GetUserByName = string -> DbResult<UserReadModel>

module Commands =
  type DbWriteError =
    | WriteError

  type DbWriteResult = Task<Result<unit, DbWriteError>>

  type ListingCreateModel = {
    ListingId: ListingId
    UserId: UserId
    Author: Author
    Title: Title
    InitialStatus: ListingStatus
  }

  type UserCreateModel = {
    UserId: UserId
    Name: string
  }

  type ListingUpdateModel = {
    ListingId: ListingId
    Author: Author
    Title: Title
    Status: ListingStatus
  }

  type CreateUser = UserCreateModel -> DbWriteResult
  type CreateListing = ListingCreateModel -> DbWriteResult
  type UpdateListing = ListingUpdateModel -> DbWriteResult
