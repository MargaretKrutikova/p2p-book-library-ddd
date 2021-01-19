module Core.EventStore

open System
open System.Threading.Tasks
open Core.Events

type EventId = Guid
type StreamId = Guid
   
type EventRead = { Id: EventId; StreamId: StreamId; CreatedAtUtc: DateTime; Data: DomainEvent }
type EventWrite = { StreamId: StreamId; Data: DomainEvent }

type EventStoreError =
    | DbError
    | ExpectedVersionMismatch

type EventStoreReadResult = Task<Result<EventRead list, EventStoreError>>
type EventStore = {
    Get: unit -> EventStoreReadResult
    GetStream: StreamId -> EventStoreReadResult
    Append: EventWrite list -> Task<Result<unit, EventStoreError>>
}

