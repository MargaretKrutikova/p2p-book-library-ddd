module Api.CompositionRoot

open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

type CompositionRoot = {
    CommandHandler: CommandHandler
    GetAllPublishedListings: GetAllPublishedBookListings    
}

let compose (commandPersistence: CommandPersistenceOperations) (queryPersistence: QueryPersistenceOperations): CompositionRoot = 
  {
      CommandHandler = handleCommand commandPersistence
      GetAllPublishedListings = getAllPublishedBookListings queryPersistence.GetAllListings
  }
