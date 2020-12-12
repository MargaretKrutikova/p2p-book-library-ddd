module BookListing.Implementation

open BookListing.Domain
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

module CreateBookListingWorkflow =
  let toDomainError (domainError: BookListingError) (error: Persistence.Queries.DbReadError): BookListingError =
    match error with
    | Persistence.Queries.MissingRecord -> domainError

  let createBookListing: Public.CreateBookListing =
    fun getUserById createListing command ->
      taskResult {
        let! user = 
          UserId.create command.BookListing.UserId 
            |> getUserById
            |> TaskResult.mapError (toDomainError UserDoesntExist)

        let form = command.BookListing

        let! title = 
          Title.create form.Title |> Result.mapError ValidationError
        let! author = 
          Author.create form.Author |> Result.mapError ValidationError

        let listingId = ListingId.create form.NewListingId
        let createListingModel: Persistence.Commands.CreateListingModel = {
          ListingId = listingId
          UserId = user.Id
          Author = author
          Title = title
          InitialStatus = Available
        }

        do! createListing createListingModel

        let bookListing = Persistence.Commands.fromCreateListingModel createListingModel
        let bookListingCreatedEvent: Events.BookListingCreated = {
          Listing = bookListing
        }
        
        return! Ok([Events.BookListingCreated bookListingCreatedEvent])
      }
      
module BorrowBookWorkflow =
  type BorrowBook = 
    (ListingId -> BookListing) -> Commands.BorrowBook -> Result<Events.BookListingEvent list, unit>

  let handleBorrowBook: BorrowBook =
    fun fetchListing command ->
      let form = command.BorrowBookForm
      // TODO: validation
      let existingListing = ListingId.create form.ListingId |> fetchListing
      
      match existingListing.Status with
      | Available ->
        let borrowedListing = { existingListing with Status = Borrowed }
        let event: Events.BookBorrowed =
          { Listing = borrowedListing; BorrowerId = UserId form.BorrowerId }

        Ok([Events.BookBorrowed event])
      | _ -> Ok([])
