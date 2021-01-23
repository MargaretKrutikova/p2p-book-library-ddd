open System
open Core.Domain.Types
open Npgsql
open FsToolkit.ErrorHandling

let createUser (dbConnection) =
    let id = Guid.NewGuid()
    Persistence.Database.CommandPersistenceImpl.createUser
        dbConnection
        { Name = "John"
          UserId = UserId.create id
          Email = ""
          UserSettings = { IsSubscribedToUserListingActivity = true } }
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    id

let createListing (dbConnection) userId =
    let id = Guid.NewGuid()

    let listing: BookListing =
        { ListingId = ListingId.create id
          Author =
              Author.create "Test"
              |> Result.defaultWith (fun _ -> failwith "")
          Title =
              Title.create "Test title"
              |> Result.defaultWith (fun _ -> failwith "")
          OwnerId = userId
          Status = Available }

    Persistence.Database.CommandPersistenceImpl.createListing dbConnection listing
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    id

let testDb () =
    let connectionString: string = ""
    let dbConnection = new NpgsqlConnection(connectionString)

    let userId = createUser dbConnection

    let _ =
        createListing dbConnection (userId |> UserId.create)

    Persistence.Database.QueryPersistenceImpl.getListingsByUserId dbConnection (userId |> UserId.create)
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> printfn "%A"

[<EntryPoint>]
let main (_) = 0 // return an integer exit code
