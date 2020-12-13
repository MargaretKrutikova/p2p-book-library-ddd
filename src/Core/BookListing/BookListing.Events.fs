module Core.BookListing.Events

type BookListingCreated = {
    Listing: Domain.BookListing
  }
  
type BookBorrowed = {
  Listing: Domain.BookListing
  BorrowerId: Domain.UserId
}

type BookListingEvent =
  | BookListingCreated of BookListingCreated
  | BookBorrowed of BookBorrowed
