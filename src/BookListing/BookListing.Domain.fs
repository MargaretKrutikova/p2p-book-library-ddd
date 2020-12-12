module BookListing.Domain
open System

// ======================================================
// Value types
// ======================================================

// TODO: enforce business rules on the type system level

type ValidationError =
  | TitleCantBeEmpty
  | TitleTooLong
  | AuthorCantBeEmpty
  | AuthorTooLong

type BookListingError =
  | UserDoesntExist
  | ValidationError of ValidationError

type UserId = private UserId of Guid
type ListingId = private ListingId of Guid

type Title = private Title of string
type Author = private Author of string

// type ListingIntent = Lend | GiveAway

type ListingStatus = Available | RequestedToBorrow | Borrowed

// ======================================================
// Entities / Aggregate roots
// ======================================================

type BookListing = {
  ListingId: ListingId
  UserId: UserId
  Author: Author
  Title: Title
  Status: ListingStatus
}

// ======================================================
// Create book listing
// ======================================================

module InputTypes = 
  type CreateBookListingDto = {
    NewListingId: Guid
    UserId: Guid
    Title: string
    Author: string
  }

  type BorrowBookFormDto = {
    ListingId: Guid
    BorrowerId: Guid
  }
 
module Commands =
  type CreateBookListing = {
    BookListing: InputTypes.CreateBookListingDto
    Timestamp: DateTime
  }

  type BorrowBook = {
    BorrowBookForm: InputTypes.BorrowBookFormDto
    Timestamp: DateTime
  }

  type BookListingCommand = 
    | CreateBookListing of CreateBookListing
    | BorrowBook of BorrowBook

// ===============================
// Smart constructors
// ===============================

module Title =
  let value ((Title title)) = title
  let create value: Result<Title, ValidationError> =
    if String.IsNullOrWhiteSpace value then
      Error TitleCantBeEmpty
    elif value.Length > 200 then
      Error TitleTooLong
    else Title value |> Ok

module Author =
  let value ((Author author)) = author
  let create value: Result<Author, ValidationError> =
    if String.IsNullOrWhiteSpace value then
      Error TitleCantBeEmpty
    elif value.Length > 100 then
      Error TitleTooLong
    else Author value |> Ok

module ListingId =
  let value ((ListingId id)) = id
  let create guid = ListingId guid

module UserId =
  let value ((UserId id)) = id
  let create guid = UserId guid
