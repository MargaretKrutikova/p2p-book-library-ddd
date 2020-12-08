module BookListing.Domain
open System

// ======================================================
// Value types
// ======================================================

// TODO: enforce business rules on the type system level

type UserId = UserId of int
type ListingId = ListingId of int

type Title = Title of string

type Author = Author of string
type ListingIntent = Lend | GiveAway

type ListingStatus = Available | RequestedToBorrow | Borrowed

// ======================================================
// Entities / Aggregate roots
// ======================================================

type BookListing = {
  ListingId: ListingId
  UserId: UserId
  Author: Author
  Title: Title
  Intent: ListingIntent
  Status: ListingStatus
}

// ======================================================
// Create book listing
// ======================================================

type CreateBookListingForm = {
  UserId: UserId
  Title: Title
  Authour: Author
  Intent: ListingIntent
}

module InputTypes = 
  type UnvalidatedCreateBookListingForm = {
    UserId: int
    Title: string
    Author: string
    Intent: ListingIntent
  }

  type UnvalidatedBorrowBookForm = {
    ListingId: int
    BorrowerId: int
  }
 
module Commands =
  type CreateBookListing = {
    BookListingForm: InputTypes.UnvalidatedCreateBookListingForm
    Timestamp: DateTime
  }

  type BorrowBook = {
    BorrowBookForm: InputTypes.UnvalidatedBorrowBookForm
    Timestamp: DateTime
  }

  type BookListingCommand = 
    | CreateBookListing of CreateBookListing
    | BorrowBook of BorrowBook

module Events =
  type BookListingCreated = {
    Listing: BookListing
  }
  type BookBorrowed = {
    Listing: BookListing
    BorrowerId: UserId
  }

  type BookListingEvent =
    | BookListingCreated of BookListingCreated
    | BookBorrowed of BookBorrowed
