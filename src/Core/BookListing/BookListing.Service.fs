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

type BookListingReadResult<'a> = Task<Result<'a, BookListingError>>
type BookListingCommandResult = Task<Result<unit, BookListingError>>

module private Conversions = 
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

let private checkUserExists (getUserById: Queries.GetUserById) userId: Task<Result<unit, BookListingError>> =
  getUserById userId 
  |> TaskResult.mapError (Conversions.toDomainError UserDoesntExist)
  |> TaskResult.ignore

module CreateBookListing =
  type Composed = Domain.CreateBookListingDto -> BookListingCommandResult
  type Service = Queries.GetUserById -> Commands.CreateListing -> Composed

  let execute: Service =
    fun getUserById createListing bookListingDto ->
      taskResult {
        do! checkUserExists getUserById bookListingDto.UserId

        let! bookListing = 
          Implementation.createBookListing bookListingDto 
          |> Result.mapError DomainError
        
        do! Conversions.toCreateListingModel bookListing 
              |> createListing 
              |> TaskResult.mapError (fun _ -> ServiceError)
      }

module RequestToBorrowBook =
  type Composed = ListingId -> UserId -> BookListingCommandResult
  type Service = 
    Queries.GetUserById -> Queries.GetListingById -> Commands.UpdateListing -> Composed

  let execute: Service =
    fun getUserById getListingById updateListing listingId borrowerId ->
      taskResult {
        do! checkUserExists getUserById borrowerId

        let! existingListing =
            listingId
            |> getListingById
            |> TaskResult.mapError (Conversions.toDomainError ListingDoesntExist)
        
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

module GetUserListings =
  type Composed = UserId -> BookListingReadResult<Queries.ListingReadModel list>
  type Service = Queries.GetUserById -> Queries.GetUserListings -> Composed

  let run: Service =
    fun getUserById getListings userId -> 
      taskResult {
        do! checkUserExists getUserById userId

        return! getListings userId 
                |> TaskResult.map (Seq.toList) 
                |> TaskResult.mapError (fun _ -> ServiceError)
      }
