module Api.CompositionRoot

open Api.Actors
open Akka.Actor

open Api.Actors.EmailSenderSupervisor
open Api.Email
open Api.InMemoryPersistence
open Core.Domain
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers

open Akka.FSharp
open FsToolkit.ErrorHandling.TaskResultCE
open Microsoft.Extensions.Logging
    
let commandHandlerWithPublish (system: ActorSystem) (commandHandler: CommandHandler) (command: Messages.Command) =
    taskResult {
        let! event = commandHandler command
        publish event system.EventStream
        publish (event |> EmailSupervisorMessage.DomainEvent) system.EventStream
        return event
    }

type CompositionRoot = {
    CommandHandler: CommandHandler
    // GetAllPublishedListings: GetAllPublishedBookListings
    GetUserBookListings: GetUserBookListings
    GetUserByName: GetUserByName
}

let private createEmailActorDependencies sendEmail (infrastructurePersistence: InfrastructurePersistenceOperations): EmailSenderSupervisor.Dependencies =
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
  let sendEmail = EmailSender.sendToPickupDirectory emailPickupDirectory smtpConfig logger
  let emailDeps = createEmailActorDependencies sendEmail infrastructurePersistence
  let system =  ActorSystem.setup emailDeps logger
  
  let commandHandler = handleCommand commandPersistence |> commandHandlerWithPublish system 
  
  {
      CommandHandler = commandHandler 
      // GetAllPublishedListings = getAllPublishedBookListings queryPersistence.GetAllListings
      GetUserBookListings = getUserBookListings queryPersistence.GetListingsByUserId
      GetUserByName = getUserByName queryPersistence.GetUserByName
  }
