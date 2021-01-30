module Api.CompositionRoot

open Api.Actors

open Api.Infrastructure.EmailSender
open Core.Commands
open Services.CommandHandlers
open Services.Email.EmailSupervisor
open Services.Persistence

open Akka.Actor
open Akka.FSharp
open FsToolkit.ErrorHandling.TaskResultCE
open Microsoft.Extensions.Logging
open Services.QueryHandlers

let private publishEvents (system: ActorSystem) (events: seq<_>) =
    events
    |> Seq.iter (fun e -> publish e system.EventStream)

let commandHandlerWithPublish (system: ActorSystem) (commandHandler: CommandHandler) (command: Command) =
    taskResult {
        let! events = commandHandler command
        publishEvents system events

        events
        |> Seq.map EmailSupervisorMessage.DomainEvent
        |> publishEvents system

        return events
    }

type CompositionRoot =
    { CommandHandler: CommandHandler
      QueryHandler: QueryHandler }

let private createEmailActorDependencies sendEmail (queries: Common.CommonQueries): EmailSupervisorDependencies =
    { GetUserEmailInfo = queries.GetUserById
      GetBookListingEmailInfo = queries.GetListingById
      SendEmail = sendEmail }

let private createCommandHandlerDependencies (commandPersistence: Commands.CommandPersistence)
                                             (commonQueries: Common.CommonQueries)
                                             : CommandHandlerDependencies =
    { GetUserById = commonQueries.GetUserById
      GetListingById = commonQueries.GetListingById
      CreateListing = commandPersistence.CreateListing
      CreateUser = commandPersistence.CreateUser
      UpdateListingStatus = commandPersistence.UpdateListingStatus }

let compose (smtpConfig: SmtpConfiguration)
            (emailPickupDirectory: string)
            (logger: ILogger)
            (commandPersistence: Commands.CommandPersistence)
            (commonQueries: Common.CommonQueries)
            (queryPersistence: QueryPersistenceOperations)
            : CompositionRoot =
    let sendEmail =
        sendToPickupDirectory emailPickupDirectory smtpConfig logger

    let emailDeps =
        createEmailActorDependencies sendEmail commonQueries

    let system = ActorSystem.setup emailDeps logger

    let commandHandler =
        createCommandHandlerDependencies commandPersistence commonQueries
        |> handleCommand
        |> commandHandlerWithPublish system

    { CommandHandler = commandHandler
      QueryHandler = createQueryHandler queryPersistence }
