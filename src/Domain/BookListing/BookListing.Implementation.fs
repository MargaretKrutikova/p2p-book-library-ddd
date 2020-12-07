module BookListing.Implementation

open BookListing.Domain

let handleCreateBookListing: Workflows.CreateBookListing =
  fun unvalidatedForm ->
    Ok([])

let handleBorrowBook: Workflows.BorrowBook =
  fun unvalidatedForm ->
    Ok([])

let commandHandler (command: Commands.BookListingCommand) =
  match command with
  | Commands.CreateBookListing data -> handleCreateBookListing data
  | Commands.BorrowBook data -> handleBorrowBook data
