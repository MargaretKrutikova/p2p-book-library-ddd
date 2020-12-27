module Core.Handlers.CommandHandlers

open Core.Common.SimpleTypes
open Core.Domain

open Core.Domain.Types
open FsToolkit.ErrorHandling.TaskResultCE
open System.Threading.Tasks

type CommandResult = Task<Result<unit, Errors.AppError>>
type CommandHandler = Messages.Command -> CommandResult

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

  type CreateListing = BookListing -> DbWriteResult
  type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult

type CommandPersistenceOperations = {
  GetUserById: CommandPersistenceOperations.GetUserById
  CreateListing: CommandPersistenceOperations.CreateListing
}

type PublishBookListing = CommandPersistenceOperations.GetUserById -> CommandPersistenceOperations.CreateListing -> Messages.PublishBookListingArgs -> CommandResult
let publishBookListing: PublishBookListing =
    fun getUserById createListing args ->
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
    | Messages.PublishBookListing args ->
      publishBookListing persistence.GetUserById persistence.CreateListing args
    | _ -> failwith ""
