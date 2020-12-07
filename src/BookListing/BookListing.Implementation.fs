module BookListing.Implementation

open BookListing.Domain

let handleCreateBookListing: Workflows.CreateBookListing =
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

let handleBorrowBook: Workflows.BorrowBook =
  fun command ->
    let form = command.BorrowBookForm
    // TODO: validation
    let bookBorrowedEvent: Events.BookBorrowed = {
      ListingId = ListingId form.ListingId
      BorrowerId = UserId form.BorrowerId
    }
    Ok([Events.BookBorrowed bookBorrowedEvent])

let commandHandler (command: Commands.BookListingCommand) =
  match command with
  | Commands.CreateBookListing data -> handleCreateBookListing data
  | Commands.BorrowBook data -> handleBorrowBook data
