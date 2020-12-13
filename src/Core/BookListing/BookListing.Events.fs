module Core.BookListing.Events

open Core.Common.SimpleTypes
open Domain

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
