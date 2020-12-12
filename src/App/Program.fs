open System
open Npgsql
open BookListing
open InMemoryPersistence

let createUser (dbConnection) =
    let id = Guid.NewGuid()
    Persistence.Database.Commands.createUser dbConnection { Name = "Jogn"; Id = id }
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    id

let createListing (dbConnection) userId =
    let id = Guid.NewGuid()
    let listing: Persistence.Database.Commands.CreateListingModel = {
        Id = id
        Author = "Test"
        Title = "Test title"
        UserId = userId
    }
    Persistence.Database.Commands.createListing dbConnection listing
    |> Async.AwaitTask
    |> Async.RunSynchronously
    |> ignore

    id

let testDb () = 
    let connectionString : string = ""
        
    let dbConnection = new NpgsqlConnection (connectionString)
    
    let userId = createUser dbConnection
    let listingId = createListing dbConnection userId
    
    Persistence.Database.Queries.getUserListings dbConnection userId
        |> Async.AwaitTask
        |> Async.RunSynchronously 
        |> printfn "%A"

let createListingCommandData (userId: Guid): Domain.Commands.CreateBookListing =
    {
        BookListing = { 
            NewListingId = Guid.NewGuid ()
            UserId = userId
            Title = "Test title"
            Author= "Test author"
        }
        Timestamp = DateTime.Now
    }

[<EntryPoint>]
let main (_) =
    let persistence = new InMemoryPersistence()
    let result =
        createListingCommandData (Guid.NewGuid())
        |> Domain.Commands.CreateBookListing
        |> CommandHandler.commandHandler persistence
        |> Async.AwaitTask
        |> Async.RunSynchronously
    
    printfn "%A" result
    
    0 // return an integer exit code
