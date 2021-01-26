module Api.Actors.EmailSenderSupervisor

open Core.Events
open Services.Email.EmailSupervisor
open Services.Email.EmailSender

open Akka.Actor
open Akka.FSharp

let private emailSenderSupervisor (dependencies: EmailSupervisorDependencies) (mailbox: Actor<EmailSupervisorMessage>) =
    let emailActor =
        handleEmailSenderMessage dependencies.SendEmail
        |> actorOf
        |> spawn mailbox "email-sender"
    
    let handleDomainEvent (event: Event) =
        match event with
        | Event.ListingRequestedToBorrow args -> sendRequestToBorrowEmail dependencies args |!> mailbox.Self
        | Event.UserRegistered args -> mailbox.Self <! sendUserRegistrationEmail dependencies args 
        | _ -> ()
            
    let rec loop () = actor {
        let! message = mailbox.Receive()
        match message with
        | DomainEvent eventEnvelope -> handleDomainEvent eventEnvelope.Event 
        | EmailInfoReady msg -> emailActor <! msg
        | FailedToFetchEmailInfo (_, e) ->
            // TODO: log error and possibly retry
            ()
        
        return! loop ()
    }
    loop ()

let createActor (dependencies: EmailSupervisorDependencies) (system: ActorSystem) =
    spawn system "email-sender-supervisor" (emailSenderSupervisor dependencies)