module Core.Handlers.CommandHandlers

open System.Data
open System.Threading.Tasks
open Core.Domain
open Core.Domain.Messages
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

open Core.Domain.Errors
open Core.Domain.Types

type CommandResult = Task<Result<unit, AppError>>
type CommandHandler = Command -> CommandResult

module CommandPersistenceOperations = 
  type DbReadError =
    | MissingRecord

  type DbResult<'a> = Task<Result<'a, DbReadError>>
  type UserReadModel = {
    Id: UserId
    Name: string
  }
  
  type ListingReadModel = {
    Id: ListingId
    OwnerId: UserId
    ListingStatus: ListingStatus
  }
  
  type GetUserById = UserId -> DbResult<UserReadModel>
  type GetListingById = ListingId -> DbResult<ListingReadModel>
  
  type DbWriteError =
    | WriteError

  type DbWriteResult = Task<Result<unit, DbWriteError>>
  type CreateUser = User -> DbWriteResult
  type CreateListing = BookListing -> DbWriteResult
  type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult

type CommandPersistenceOperations = {
  GetUserById: CommandPersistenceOperations.GetUserById
  GetListingById: CommandPersistenceOperations.GetListingById
  CreateListing: CommandPersistenceOperations.CreateListing
  CreateUser: CommandPersistenceOperations.CreateUser
  UpdateListingStatus: CommandPersistenceOperations.UpdateListingStatus
}

let private checkUserExists (getUserById: CommandPersistenceOperations.GetUserById) userId: Task<Result<unit, AppError>> =
  getUserById userId 
  |> TaskResult.mapError (function | CommandPersistenceOperations.MissingRecord -> Validation UserNotFound)
  |> TaskResult.ignore

let private mapFromDbListingError = function
    | CommandPersistenceOperations.MissingRecord -> Validation BookListingNotFound

type PublishBookListing = CommandPersistenceOperations.GetUserById -> CommandPersistenceOperations.CreateListing -> PublishBookListingArgs -> CommandResult
let publishBookListing: PublishBookListing =
    fun getUserById createListing args ->
    taskResult {
      do! checkUserExists getUserById args.UserId

      let! bookListing = Logic.publishBookListing args 
      do! createListing bookListing |> TaskResult.mapError (fun _ -> ServiceError)
    }

type RegisterUser = CommandPersistenceOperations.CreateUser -> RegisterUserArgs -> CommandResult    
let registerUser: RegisterUser =
  fun createUser args ->
     taskResult {
        let user: User = {
          UserId = args.UserId
          Name = args.Name
        }
        do! createUser user |> TaskResult.mapError (fun _ -> ServiceError)
     }

type RequestToBorrowBook = CommandPersistenceOperations -> RequestToBorrowListingArgs -> CommandResult
let requestToBorrowBook: RequestToBorrowBook =
  fun persistence args ->
    taskResult {
      do! checkUserExists persistence.GetUserById args.BorrowerId
      let! listing = persistence.GetListingById args.ListingId |> TaskResult.mapError mapFromDbListingError
      
      let! newStatus = Logic.borrowListing args.BorrowerId listing.ListingStatus
      do! persistence.UpdateListingStatus listing.Id newStatus |> TaskResult.mapError (fun _ -> ServiceError)
      return ()  
    }

let handleCommand (persistence: CommandPersistenceOperations): CommandHandler =
  fun command ->
    match command with
    | Command.RegisterUser args -> registerUser persistence.CreateUser args
    | Command.PublishBookListing args -> publishBookListing persistence.GetUserById persistence.CreateListing args
    | Command.RequestToBorrowBook args -> requestToBorrowBook persistence args
    | _ -> failwith ""
