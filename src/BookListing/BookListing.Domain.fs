module BookListing.Domain
open System

// ======================================================
// Common types
// ======================================================

type UserId = UserId of int
type ListingId = ListingId of int

type Title = Title of string
type Author = Author of string
type ListingIntent = Lend | GiveAway

type ListingStatus = Available | Borrowed

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
    ListingId: ListingId
    BorrowerId: UserId
  }

  type BookListingEvent =
    | BookListingCreated of BookListingCreated
    | BookBorrowed of BookBorrowed

module Workflows =
  type CreateBookListing = 
    Commands.CreateBookListing -> Result<Events.BookListingEvent list, unit>
  
  type BorrowBook =
    Commands.BorrowBook -> Result<Events.BookListingEvent list, unit>
