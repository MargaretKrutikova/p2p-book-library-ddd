module Core.Handlers.CommandHandlers

open System.Threading.Tasks
open Core.Domain.Messages
open FsToolkit.ErrorHandling.TaskResultCE

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

  type UserCreateModel = {
    UserId: UserId
    Name: string
  }

  type CreateUser = UserCreateModel -> DbWriteResult
  type CreateListing = BookListing -> DbWriteResult
  type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult

type CommandPersistenceOperations = {
  GetUserById: CommandPersistenceOperations.GetUserById
  CreateListing: CommandPersistenceOperations.CreateListing
  CreateUser: CommandPersistenceOperations.CreateUser
}

type PublishBookListing = CommandPersistenceOperations.GetUserById -> CommandPersistenceOperations.CreateListing -> PublishBookListingArgs -> CommandResult
let publishBookListing: PublishBookListing =
    fun getUserById createListing args ->
    taskResult {
      return ()
    }

type RegisterUser = CommandPersistenceOperations.CreateUser -> RegisterUserArgs -> CommandResult    
let registerUser: RegisterUser =
  fun createUser args ->
     taskResult {
       return ()
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
