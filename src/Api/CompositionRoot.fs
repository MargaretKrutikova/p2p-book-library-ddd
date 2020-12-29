module Api.CompositionRoot

open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

type CompositionRoot = {
    CommandHandler: CommandHandler
    // GetAllPublishedListings: GetAllPublishedBookListings
    GetUserBookListings: GetUserBookListings
    GetUserByName: GetUserByName
}

let compose (commandPersistence: CommandPersistenceOperations) (queryPersistence: QueryPersistenceOperations): CompositionRoot = 
  {
      CommandHandler = handleCommand commandPersistence
      // GetAllPublishedListings = getAllPublishedBookListings queryPersistence.GetAllListings
      GetUserBookListings = getUserBookListings queryPersistence.GetListingsByUserId
      GetUserByName = getUserByName queryPersistence.GetUserByName
  }
