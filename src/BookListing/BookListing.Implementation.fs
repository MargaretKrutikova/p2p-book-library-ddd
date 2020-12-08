module BookListing.Implementation

open BookListing.Domain

module CreateBookListingWorkflow =
  type CreateBookListing = 
    Commands.CreateBookListing -> Result<Events.BookListingEvent list, unit>

  let createBookListing: CreateBookListing =
    fun command ->
      let form = command.BookListingForm
      // TODO: validation
      let bookListingCreated: BookListing = {
        ListingId = ListingId 42 // TODO: haha
        UserId = UserId form.UserId
        Author = Author form.Author
        Title = Title form.Title
        Intent = form.Intent
        Status = Available
      }
      let bookListingCreatedEvent: Events.BookListingCreated = {
        Listing = bookListingCreated
      }
      
      Ok([Events.BookListingCreated bookListingCreatedEvent])

module BorrowBookWorkflow =
  type BorrowBook = 
    (ListingId -> BookListing) -> Commands.BorrowBook -> Result<Events.BookListingEvent list, unit>

  let handleBorrowBook: BorrowBook =
    fun fetchListing command ->
      let form = command.BorrowBookForm
      // TODO: validation
      let existingListing = fetchListing (ListingId form.ListingId)
      
      match existingListing.Status with
      | Available ->
        let borrowedListing = { existingListing with Status = Borrowed }
        let event: Events.BookBorrowed =
          { Listing = borrowedListing; BorrowerId = UserId form.BorrowerId }

        Ok([Events.BookBorrowed event])
      | _ -> Ok([])
