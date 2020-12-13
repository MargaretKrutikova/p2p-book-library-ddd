module Core.BookListing.Persistence

open Core.BookListing.Domain
open Core.Common.SimpleTypes

open System
open System.Threading.Tasks

module Queries =
  type DbReadError =
    | MissingRecord

  type Listing = {
    ListingId: ListingId
    UserId: UserId
    Author: Author
    Title: Title
    Status: ListingStatus
    PublishedDate: DateTime
  }

  let fromGetListingModel (model: Listing): BookListing =
    {
      ListingId = model.ListingId
      UserId = model.UserId
      Author = model.Author
      Title = model.Title
      Status = model.Status
    }
  
  type User = {
    Id: UserId
    Name: string
  }

  type GetListingById = ListingId -> Task<Result<Listing, DbReadError>>
  type GetUserListings = UserId -> Task<Listing seq>
  type GetUserById = UserId -> Task<Result<User, DbReadError>>

module Commands =
  type CreateListingModel = {
    ListingId: ListingId
    UserId: UserId
    Author: Author
    Title: Title
    InitialStatus: ListingStatus
  }

  let fromCreateListingModel (model: CreateListingModel): BookListing =
    {
      ListingId = model.ListingId
      UserId = model.UserId
      Author = model.Author
      Title = model.Title
      Status = model.InitialStatus
    }

  type CreateUserModel = {
    UserId: UserId
    Name: string
  }

  // TODO: error handling
  type CreateUserCommand = CreateUserModel -> Task<unit>
  type CreateListing = CreateListingModel -> Task<unit>
