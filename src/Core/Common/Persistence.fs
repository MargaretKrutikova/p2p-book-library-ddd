module Core.Common.Persistence

open Core.Common.SimpleTypes

open System
open System.Threading.Tasks

module Queries =
  type DbReadError =
    | MissingRecord

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

  type GetListingById = ListingId -> Task<Result<ListingReadModel, DbReadError>>
  type GetUserListings = UserId -> Task<ListingReadModel seq>
  type GetUserById = UserId -> Task<Result<UserReadModel, DbReadError>>

module Commands =
  type DbWriteError =
    | WriteError

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

  type CreateUser = UserCreateModel -> Task<Result<unit, DbWriteError>>
  type CreateListing = ListingCreateModel -> Task<Result<unit, DbWriteError>>
  type UpdateListing = ListingUpdateModel -> Task<Result<unit, DbWriteError>>
