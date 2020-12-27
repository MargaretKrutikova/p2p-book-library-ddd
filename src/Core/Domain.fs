namespace Core.Domain

open Core.Common.SimpleTypes

module Messages =
  type PublishBookListingArgs = {
    NewListingId: ListingId
    UserId: UserId
    Title: string
    Author: string
  }

  type RequestToBorrowBookArgs = {
    ListingId: ListingId
    BorrowerId: UserId
  }

  type BorrowBookArgs = {
    ListingId: ListingId
    BorrowerId: UserId
  }

  type Command = 
    | PublishBookListing of PublishBookListingArgs
    | RequestToBorrowBook of RequestToBorrowBookArgs
    | BorrowBook of BorrowBookArgs

  type Event =
    | BookListingPublished of ListingId
    | RequestedToBorrowBook of ListingId * UserId
    | BorrowedBook of ListingId * UserId

  type Query =
    | GetAllPublishedBookListings
    | GetUsersPublishedBookListings of UserId

module Types =
  type Title = private Title of string
  type Author = private Author of string

  type ListingStatus = Available | RequestedToBorrow | Borrowed

  type BookListing = {
    ListingId: ListingId
    UserId: UserId
    Author: Author
    Title: Title
    Status: ListingStatus
  }

module Errors =
  type ValidationError = 
    | TitleIsBeEmpty
    | AuthorIsEmpty

  type DomainError =
    | BookListingDoesntExist
    | UserDoesntExist
    | CantRequestBorrow
    | CantBorrowBeforeRequestIsApproved

  type AppError =
    | Validation of ValidationError
    | Domain of DomainError
