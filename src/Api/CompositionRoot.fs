module Api.CompositionRoot

open Api.Actors
open Akka.Actor

open Core.Domain
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

open Akka.FSharp
open FsToolkit.ErrorHandling.TaskResultCE
    
let commandHandlerWithPublish (system: ActorSystem) (commandHandler: CommandHandler) (command: Messages.Command) =
    taskResult {
        let! events = commandHandler command
        publish events system.EventStream
        return events
    }

type CompositionRoot = {
    CommandHandler: CommandHandler
    // GetAllPublishedListings: GetAllPublishedBookListings
    GetUserBookListings: GetUserBookListings
    GetUserByName: GetUserByName
}

let compose (commandPersistence: CommandPersistenceOperations) (queryPersistence: QueryPersistenceOperations): CompositionRoot = 
  let system = setupActors ()
  let commandHandler = handleCommand commandPersistence |> commandHandlerWithPublish system 
  
  {
      CommandHandler = commandHandler 
      // GetAllPublishedListings = getAllPublishedBookListings queryPersistence.GetAllListings
      GetUserBookListings = getUserBookListings queryPersistence.GetListingsByUserId
      GetUserByName = getUserByName queryPersistence.GetUserByName
  }
