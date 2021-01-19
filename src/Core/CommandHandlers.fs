module Core.Handlers.CommandHandlers

open System.Threading.Tasks
open Core.EventStore
open Core.Events
open Core.Logic
open Core.Commands
open Core.Persistence
open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

open Core.Domain.Errors
open Core.Domain.Types

type CommandResult = Task<Result<DomainEvent option, AppError>>
type CommandHandler = Command -> CommandResult

let private checkUserExists (getUserById: GetUserById) userId: Task<Result<unit, AppError>> =
    getUserById userId
    |> TaskResult.mapError (function | MissingRecord -> Validation UserNotFound)
    |> TaskResult.ignore

let private mapFromEventStoreError =
    function
        | DbError | ExpectedVersionMismatch -> ServiceError

let publishBookListing (operations: Persistence) (store: EventStore) (args: PublishBookListingArgs): CommandResult = 
    taskResult {
        do! checkUserExists operations.GetUserById args.UserId
        let! bookListing = publishBookListing args

        let domainEvent = DomainEvent.BookListingPublished { Listing = bookListing }
        let event = { Data = domainEvent; StreamId = bookListing.ListingId |> ListingId.value }
        
        do! store.Append [event] |> TaskResult.mapError mapFromEventStoreError
        return Some domainEvent
    }

let registerUser (persistence: Persistence) (args: RegisterUserArgs): CommandResult =
    taskResult {
        let user: User = { UserId = args.UserId; Name = args.Name }
        do! persistence.CreateUser user |> TaskResult.mapError (fun _ -> ServiceError)
        return None
    }

let changeListingStatus (persistence: Persistence) (store: EventStore) (args: ChangeListingStatusArgs): CommandResult =
    taskResult {
        do! checkUserExists persistence.GetUserById args.ChangeRequestedByUserId
        let! listingEvents =
            args.ListingId
            |> ListingId.value
            |> store.GetStream
            |> TaskResult.map (List.map (fun event -> event.Data))
            |> TaskResult.mapError mapFromEventStoreError

        let! listing =
            List.fold Projections.applyEvent None listingEvents
            |> Result.requireSome (Validation ListingNotFound)
            
        let! event = executeStatusChangeCommand listing args
        let eventWrite = { Data = event; StreamId = listing.ListingId |> ListingId.value }

        do! store.Append [eventWrite] |> TaskResult.mapError mapFromEventStoreError
        return event |> Some
    }

let handleCommand (persistence: Persistence) (store: EventStore): CommandHandler =
    fun command ->
        match command with
        | Command.RegisterUser args -> registerUser persistence args
        | Command.PublishBookListing args -> publishBookListing persistence store args
        | Command.ChangeListingStatus args -> changeListingStatus persistence store args
