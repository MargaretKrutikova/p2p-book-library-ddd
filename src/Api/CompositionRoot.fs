module Api.CompositionRoot

open Api.Actors

open Api.InMemoryPersistence
open Api.Infrastructure.EmailSender
open Core.Commands
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers
open Services.Email.EmailSupervisor

open Akka.Actor
open Akka.FSharp
open FsToolkit.ErrorHandling.TaskResultCE
open Microsoft.Extensions.Logging
open Services.Email.Types
    
let commandHandlerWithPublish (system: ActorSystem) (commandHandler: CommandHandler) (command: Command) =
    taskResult {
        let! event = commandHandler command
        publish event system.EventStream
        publish (event |> Seq.map EmailSupervisorMessage.DomainEvent) system.EventStream
        return event
    }

type CompositionRoot = {
    CommandHandler: CommandHandler
    QueryHandler: QueryHandler
}

let private createEmailActorDependencies sendEmail (infrastructurePersistence: InfrastructurePersistenceOperations): EmailSenderDependencies =
    {
        GetUserEmailInfo = infrastructurePersistence.GetUserEmailInfo
        GetBookListingEmailInfo = infrastructurePersistence.GetBookListingEmailInfo
        SendEmail =  sendEmail
    }

let compose
    (smtpConfig: SmtpConfiguration)
    (emailPickupDirectory: string)
    (logger: ILogger)
    (commandPersistence: CommandPersistenceOperations)
    (queryPersistence: QueryPersistenceOperations)
    (infrastructurePersistence: InfrastructurePersistenceOperations): CompositionRoot =
  let sendEmail = sendToPickupDirectory emailPickupDirectory smtpConfig logger
  let emailDeps = createEmailActorDependencies sendEmail infrastructurePersistence
  let system =  ActorSystem.setup emailDeps logger
  
  let commandHandler = handleCommand commandPersistence |> commandHandlerWithPublish system 
  
  {
      CommandHandler = commandHandler
      QueryHandler = createQueryHandler queryPersistence
  }
