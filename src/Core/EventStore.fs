module Core.EventStore

open System
open System.Threading.Tasks

type EventId = Guid
type StreamId = Guid
type EventRead = { Id: EventId; StreamId: StreamId; CreatedAtUtc: DateTime; Name: string; Data: string }
type EventWrite = { StreamId: StreamId; Data: string; Name: string }

type EventStoreError =
    | DbError
    | ExpectedVersionMismatch

type EventStoreReadResult = Task<Result<EventRead list, EventStoreError>>
type EventStore = {
    Get: unit -> EventStoreReadResult
    GetStream: StreamId -> EventStoreReadResult
    Append: EventWrite list -> Task<Result<unit, EventStoreError>>
}

