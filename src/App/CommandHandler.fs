module CommandHandler

open BookListing
open InMemoryPersistence

let private handleCreateBookListing (persistence: InMemoryPersistence) command =
  Implementation.createBookListing persistence.GetUserById persistence.CreateListing command

let private handleBorrowBook (persistence: InMemoryPersistence) command =
  Implementation.borrowBook persistence.GetListingById command

let commandHandler (persistence: InMemoryPersistence) (command: Domain.Commands.BookListingCommand) =
    match command with
    | Domain.Commands.CreateBookListing data -> handleCreateBookListing persistence data
    | Domain.Commands.BorrowBook data -> handleBorrowBook persistence data
  