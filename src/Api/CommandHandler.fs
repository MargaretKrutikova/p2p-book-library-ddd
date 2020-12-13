module Api.CommandHandler

open Core.BookListing
open Api.InMemoryPersistence

let private handleCreateBookListing (persistence: Persistence) command =
  Implementation.createBookListing persistence.GetUserById persistence.CreateListing command

let private handleBorrowBook (persistence: Persistence) command =
  Implementation.borrowBook persistence.GetListingById command

let commandHandler (persistence: Persistence) (command: Domain.Commands.BookListingCommand) =
    // TODO: send events to some message bus?
    match command with
    | Domain.Commands.CreateBookListing data -> handleCreateBookListing persistence data
    | Domain.Commands.BorrowBook data -> handleBorrowBook persistence data
  