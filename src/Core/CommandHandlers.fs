module Core.Handlers.CommandHandlers

open System.Threading.Tasks
open Core.EventStore
open Core.Events
open Core.Commands
open Core.Persistence
open Core.Domain.Errors
open Core.Aggregate
open Core.Domain.Types

open FsToolkit.ErrorHandling.TaskResultCE
open FsToolkit.ErrorHandling

module Mapping =
    open Newtonsoft.Json

    let toStoredEvent event =
        match event with
        | BookListingPublished args -> "BookListingPublished", args |> JsonConvert.SerializeObject
        | ListingRequestedToBorrow args -> "ListingRequestedToBorrow", args |> JsonConvert.SerializeObject
        | RequestToBorrowCancelled args -> "RequestToBorrowCancelled", args |> JsonConvert.SerializeObject
        | RequestToBorrowApproved args -> "RequestToBorrowApproved", args |> JsonConvert.SerializeObject
        | ListingReturned args -> "ListingReturned", args |> JsonConvert.SerializeObject
    
    let toDomainEvent (event: EventRead) =
        match event.Name, event.Data with
        | "BookListingPublished", args ->
            args |> JsonConvert.DeserializeObject<BookListingPublishedEventArgs> |> BookListingPublished
        | "ListingRequestedToBorrow", args ->
            args |> JsonConvert.DeserializeObject<ListingRequestedToBorrowEventArgs> |> ListingRequestedToBorrow
        | "RequestToBorrowCancelled", args ->
            args |> JsonConvert.DeserializeObject<RequestToBorrowCancelledEventArgs> |> RequestToBorrowCancelled
        | "RequestToBorrowApproved", args ->
            args |> JsonConvert.DeserializeObject<RequestToBorrowApprovedEventArgs> |> RequestToBorrowApproved
        | "ListingReturned", args ->
            args |> JsonConvert.DeserializeObject<ListingReturnedEventArgs> |> ListingReturned
        | _ -> failwith "Unknown event"    
    
    let toEventWrite aggregateId event =
        let name, data = toStoredEvent event
        { Data = data; Name = name; StreamId = aggregateId }
        
type CommandResult = Task<Result<DomainEvent list, AppError>>
type CommandHandler = ListingId -> Command -> CommandResult

let private checkUserExists (getUserById: GetUserById) userId: Task<Result<unit, AppError>> =
    getUserById userId
    |> TaskResult.mapError (function | MissingRecord -> AppError.Validation UserNotFound)
    |> TaskResult.ignore

let private mapFromEventStoreError =
    function | DbError | ExpectedVersionMismatch -> ServiceError

// TODO: move outside into infrastructure
let private validate (persistence: Persistence) command =
     match command with
     | Command.PublishBookListing args ->
        checkUserExists persistence.GetUserById args.UserId
     | Command.ChangeListingStatus args ->
        checkUserExists persistence.GetUserById args.ChangeRequestedByUserId

let getCurrentState (store: EventStore) (aggregateId: StreamId) =
    let mapper = Seq.map Mapping.toDomainEvent >> Seq.fold listingAggregate.Apply listingAggregate.Init
    aggregateId
    |> store.GetStream
    |> TaskResult.map mapper
    |> TaskResult.mapError mapFromEventStoreError

let handleCommand (persistence: Persistence) (store: EventStore): CommandHandler =
    fun listingId command ->
        taskResult {
            do! validate persistence command
            
            let aggregateId = ListingId.value listingId
            let! state = getCurrentState store aggregateId 
            let! events = listingAggregate.Execute state command
            
            do! events
                |> List.map (Mapping.toEventWrite aggregateId)
                |> store.Append
                |> TaskResult.mapError mapFromEventStoreError
                
            return events
        }
            
        
