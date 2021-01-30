module Services.CommandHandlers

open System
open System.Threading.Tasks
open Core.Events
open Core.Logic
open Core.Commands
open Core.Domain.Errors
open Core.Domain.Types

open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling
open Services.Persistence

type CommandResult = Task<Result<EventEnvelope list, AppError>>
type CommandHandler = Command -> CommandResult

type CommandHandlerDependencies =
    { GetUserById: Common.GetUserById
      GetListingById: Common.GetListingById
      CreateListing: Commands.CreateListing
      CreateUser: Commands.CreateUser
      UpdateListingStatus: Commands.UpdateListingStatus }

let private mapFromDbListingError =
    function
    | DbReadError.MissingRecord -> AppError.Validation ListingNotFound
    | DbReadError.InternalError -> AppError.ServiceError

let private mapFromDbUserError =
    function
    | DbReadError.MissingRecord -> AppError.Validation UserNotFound
    | DbReadError.InternalError -> AppError.ServiceError

let private checkUserExists (getUserById: Common.GetUserById) userId: Task<Result<unit, AppError>> =
    getUserById userId
    |> TaskResult.mapError mapFromDbUserError
    |> TaskResult.ignore

let publishBookListing (operations: CommandHandlerDependencies) (args: PublishBookListingArgs): CommandResult = 
    taskResult {
        do! checkUserExists operations.GetUserById args.UserId
        let! bookListing = publishBookListing args
        
        do! operations.CreateListing bookListing |> TaskResult.mapError (fun _ -> ServiceError)
        let event = Event.BookListingPublished { Listing = bookListing }
        
        return { Timestamp = DateTime.UtcNow; Event = event } |> List.singleton
    }

type RegisterUser = Commands.CreateUser -> RegisterUserArgs -> CommandResult    
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

let executeChangeStatusCommand (dependencies: CommandHandlerDependencies) (args: ChangeListingStatusArgs): CommandResult =
    taskResult {
        do! checkUserExists dependencies.GetUserById args.ChangeRequestedByUserId
        let! listing =
            dependencies.GetListingById args.ListingId
            |> TaskResult.mapError mapFromDbListingError

        let! (event, newStatus) = changeListingStatus listing args
        do! dependencies.UpdateListingStatus listing.Id newStatus
            |> TaskResult.mapError (fun _ -> ServiceError)
        
        return { Timestamp = DateTime.UtcNow; Event = event } |> List.singleton
    }

let handleCommand (dependencies: CommandHandlerDependencies): CommandHandler =
    fun command ->
        match command with
        | Command.RegisterUser args -> registerUser dependencies.CreateUser args
        | Command.PublishBookListing args -> publishBookListing dependencies args
        | Command.ChangeListingStatus args -> executeChangeStatusCommand dependencies args
