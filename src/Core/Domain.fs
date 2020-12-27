namespace Core.Domain

open System

module Errors =
  type ValidationError = 
    | TitleIsEmpty
    | TitleTooLong
    | AuthorTooLong
    | AuthorIsEmpty
    | UserNotFound
    | BookListingNotFound
    
  type DomainError =
    | CantRequestBorrow
    | CantBorrowBeforeRequestIsApproved

  type AppError =
    | Validation of ValidationError
    | Domain of DomainError

module Types =
  type UserId = private UserId of Guid
  type ListingId = private ListingId of Guid
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
  
  module UserId =
    let value ((UserId id)) = id
    let create guid = UserId guid

  module ListingId =
    let value ((ListingId id)) = id
    let create guid = ListingId guid

  module Title =
    open Errors
    
    let create value: Result<Title, ValidationError> =
      if String.IsNullOrWhiteSpace value then
        Error TitleIsEmpty
      elif value.Length > 200 then
        Error TitleTooLong
      else value |> Title |> Ok

  module Author =
    open Errors

    let create value: Result<Author, ValidationError> =
      if String.IsNullOrWhiteSpace value then
        Error AuthorIsEmpty
      elif value.Length > 100 then
        Error AuthorTooLong
      else value |> Author |> Ok

module Messages =
  open Types
  
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
  
  type RegisterUserArgs = {
    UserId: UserId
    Name: string
  }

  type Command =
    | RegisterUser of RegisterUserArgs 
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
