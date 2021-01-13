module Api.CompositionRoot

open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

type CompositionRoot = {
    CommandHandler: CommandHandler
    QueryHandler: QueryHandler
}

let compose (commandPersistence: CommandPersistenceOperations) (queryPersistence: QueryPersistenceOperations): CompositionRoot = 
  {
      CommandHandler = handleCommand commandPersistence
      QueryHandler = createQueryHandler queryPersistence
  }
