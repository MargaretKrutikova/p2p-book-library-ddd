module BookListing.Implementation

open BookListing.Domain
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

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
      
let borrowBook: Public.BorrowBook =
  fun getListingById command ->
    taskResult {
      let form = command.BorrowBookForm
      // TODO: validation
      let! existingListing =
          ListingId.create form.ListingId
          |> getListingById
          |> TaskResult.mapError (toDomainError ListingDoesntExist)
      
      let events =
        match existingListing.Status with
        | ListingStatus.Available ->
          let borrowedListing = { Persistence.Queries.fromGetListingModel existingListing with Status = Borrowed }
          let event: Events.BookBorrowed =
            { Listing = borrowedListing; BorrowerId = UserId.create form.BorrowerId }

          [Events.BookBorrowed event]
        | _ -> []
        
      return events
    }
