module BookListing.Api

open BookListing.Domain
open BookListing.Implementation
open FsToolkit.ErrorHandling.ResultCE

// TODO: Take in DTOs
// DI
let handleCreateBookListing = CreateBookListingWorkflow.createBookListing
let handleBorrowBook = BorrowBookWorkflow.handleBorrowBook Persistence.getListingById

let persistEvent (event: Events.BookListingEvent) =
  match event with
  | Events.BookBorrowed data -> 
    Persistence.setListingById data.Listing 
  | Events.BookListingCreated data -> 
    Persistence.createListing data.Listing

let commandHandler (command: Commands.BookListingCommand) = 
  result {
    let! events =
      match command with
      | Commands.CreateBookListing data -> handleCreateBookListing data
      | Commands.BorrowBook data -> handleBorrowBook data
  
    events |> Seq.iter persistEvent
  }
  