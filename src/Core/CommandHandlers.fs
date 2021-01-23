module Core.Handlers.CommandHandlers

open System
open System.Threading.Tasks
open Core.Events
open Core.Logic
open Core.Commands
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

open Core.Domain.Errors
open Core.Domain.Types

type CommandResult = Task<Result<EventEnvelope list, AppError>>
type CommandHandler = Command -> CommandResult

module CommandPersistenceOperations =
    type DbReadError = | MissingRecord
    type DbResult<'a> = Task<Result<'a, DbReadError>>
    type UserReadModel = { Id: UserId; Name: string }

    type GetUserById = UserId -> DbResult<UserReadModel>
    type GetListingById = ListingId -> DbResult<ListingReadModel>

    type DbWriteError = | WriteError

    type DbWriteResult = Task<Result<unit, DbWriteError>>
    type CreateUser = User -> DbWriteResult
    type CreateListing = BookListing -> DbWriteResult
    type UpdateListingStatus = ListingId -> ListingStatus -> DbWriteResult

type CommandPersistenceOperations =
    { GetUserById: CommandPersistenceOperations.GetUserById
      GetListingById: CommandPersistenceOperations.GetListingById
      CreateListing: CommandPersistenceOperations.CreateListing
      CreateUser: CommandPersistenceOperations.CreateUser
      UpdateListingStatus: CommandPersistenceOperations.UpdateListingStatus }

let private checkUserExists (getUserById: CommandPersistenceOperations.GetUserById) userId: Task<Result<unit, AppError>> =
    getUserById userId
    |> TaskResult.mapError (function
        | CommandPersistenceOperations.MissingRecord -> Validation UserNotFound)
    |> TaskResult.ignore

let private mapFromDbListingError =
    function
    | CommandPersistenceOperations.MissingRecord -> Validation ListingNotFound

let publishBookListing (operations: CommandPersistenceOperations) (args: PublishBookListingArgs): CommandResult = 
    taskResult {
        do! checkUserExists operations.GetUserById args.UserId
        let! bookListing = publishBookListing args
        
        do! operations.CreateListing bookListing |> TaskResult.mapError (fun _ -> ServiceError)
        let event = Event.BookListingPublished { Listing = bookListing }
        
        return { Timestamp = DateTime.UtcNow; Event = event } |> List.singleton
    }

type RegisterUser = CommandPersistenceOperations.CreateUser -> RegisterUserArgs -> CommandResult    
let registerUser: RegisterUser =
  fun createUser args ->
     taskResult {
        let user: User = {
          UserId = args.UserId
          Name = args.Name
          Email = args.Email // TODO: validate
          UserSettings = {
            IsSubscribedToUserListingActivity = args.IsSubscribedToUserListingActivity
          }
        }
        do! createUser user |> TaskResult.mapError (fun _ -> ServiceError)
        return { Timestamp = DateTime.UtcNow; Event = Event.UserRegistered args } |> List.singleton
     }

let executeChangeStatusCommand (persistence: CommandPersistenceOperations) (args: ChangeListingStatusArgs): CommandResult =
    taskResult {
        do! checkUserExists persistence.GetUserById args.ChangeRequestedByUserId
        let! listing =
            persistence.GetListingById args.ListingId
            |> TaskResult.mapError mapFromDbListingError

        let! (event, newStatus) = changeListingStatus listing args
        do! persistence.UpdateListingStatus listing.Id newStatus
            |> TaskResult.mapError (fun _ -> ServiceError)
        
        return { Timestamp = DateTime.UtcNow; Event = event } |> List.singleton
    }

let handleCommand (persistence: CommandPersistenceOperations): CommandHandler =
    fun command ->
        match command with
        | Command.RegisterUser args -> registerUser persistence.CreateUser args
        | Command.PublishBookListing args -> publishBookListing persistence args
        | Command.ChangeListingStatus args -> executeChangeStatusCommand persistence args
