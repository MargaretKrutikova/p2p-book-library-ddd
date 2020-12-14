module Core.BookListing.Service

open Core.BookListing.Domain
open Core.Common.SimpleTypes
open Core.Common.Persistence

open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling
open System.Threading.Tasks

type BookListingError =
  | UserDoesntExist
  | ListingDoesntExist
  | ServiceError
  | DomainError of BookListingDomainError

type CreateBookListing = Domain.CreateBookListingDto -> Task<Result<unit, BookListingError>>
type RequestToBorrowBook = ListingId -> UserId -> Task<Result<unit, BookListingError>>
type CreateUser = CreateUserDto -> Task<Result<unit, BookListingError>>

module private Converstions = 
  let toDomainError (domainError: BookListingError) (error: Queries.DbReadError): BookListingError =
    match error with
    | Queries.MissingRecord -> domainError

  let toCreateListingModel (listing: BookListing): Commands.ListingCreateModel =
    {
      ListingId = listing.ListingId
      UserId = listing.UserId
      Author = listing.Author
      Title = listing.Title
      InitialStatus = listing.Status
    }

let createBookListing 
  (getUserById: Queries.GetUserById)
  (createListing: Commands.CreateListing): CreateBookListing =
  fun bookListingDto ->
    taskResult {
      do! bookListingDto.UserId 
            |> getUserById
            |> TaskResult.mapError (Converstions.toDomainError UserDoesntExist)
            |> TaskResult.ignore

      let! bookListing = 
        Implementation.createBookListing bookListingDto 
        |> Result.mapError DomainError
      
      do! Converstions.toCreateListingModel bookListing 
            |> createListing 
            |> TaskResult.mapError (fun _ -> ServiceError)
    }
      
let requestToBorrowBook
  (getUserById: Queries.GetUserById)
  (getListingById: Queries.GetListingById)
  (updateListing: Commands.UpdateListing): RequestToBorrowBook =
    fun listingId borrowerId ->
      taskResult {
        let! existingListing =
            listingId
            |> getListingById
            |> TaskResult.mapError (Converstions.toDomainError ListingDoesntExist)
        
        do! borrowerId
              |> getUserById
              |> TaskResult.mapError (Converstions.toDomainError UserDoesntExist)
              |> TaskResult.ignore
        
        let! updatedStatus = 
            Implementation.requestToBorrow existingListing.Status
            |> Result.mapError DomainError
        
        let updateModel: Commands.ListingUpdateModel = {
          ListingId = listingId
          Title = existingListing.Title
          Author = existingListing.Author
          Status = updatedStatus
        } 

        do! updateListing updateModel |> TaskResult.mapError (fun _ -> ServiceError)
      }

let createUser (createUser: Commands.CreateUser): CreateUser =
  fun dto -> 
    taskResult {
      let userModel: Commands.UserCreateModel = {
        UserId = dto.UserId
        Name = dto.Name
      }
      do! createUser userModel |> TaskResult.mapError (fun _ -> ServiceError)
    }
