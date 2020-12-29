module Core.Handlers.CommandHandlers

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
  type GetUserById = UserId -> DbResult<UserReadModel>

  type DbWriteError =
    | WriteError

  type DbWriteResult = Task<Result<unit, DbWriteError>>
  type CreateUser = User -> DbWriteResult
  type CreateListing = BookListing -> DbWriteResult
  type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult

type CommandPersistenceOperations = {
  GetUserById: CommandPersistenceOperations.GetUserById
  CreateListing: CommandPersistenceOperations.CreateListing
  CreateUser: CommandPersistenceOperations.CreateUser
}

let private checkUserExists (getUserById: CommandPersistenceOperations.GetUserById) userId: Task<Result<unit, AppError>> =
  getUserById userId 
  |> TaskResult.mapError (function | CommandPersistenceOperations.MissingRecord -> Validation UserNotFound)
  |> TaskResult.ignore

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

//type RequestToBorrowBook =
//  PersistenceOperations.GetUserById -> PersistenceOperations.GetListingById -> Commands.UpdateListing -> Messages.RequestToBorrowBookArgs -> CommandResult
//let requestToBorrowBook: RequestToBorrowBook =
//  fun getUserById getListingById updateListing args ->
//    taskResult {
//      return ()  
//    }

let handleCommand (persistence: CommandPersistenceOperations): CommandHandler =
  fun command ->
    match command with
    | Command.RegisterUser args -> registerUser persistence.CreateUser args
    | Command.PublishBookListing args -> publishBookListing persistence.GetUserById persistence.CreateListing args
    | _ -> failwith ""
