module Api.Actors

open Akka.Actor
open Akka.FSharp
open Api.Email
open Core.Domain
open Microsoft.Extensions.Logging

let private createEmailActor (smtpConfig: SmtpConfiguration) (logger: ILogger) (system: ActorSystem) =
    let processMessage =
        fun (mailbox: Actor<Messages.Event>) ->
            let rec loop () = actor {
               let! message = mailbox.Receive()
               match message with
               | Messages.Event.UserRegistered _->
                    EmailSender.send smtpConfig logger "test@test.com" "test" "test" "welcome"
               | _ -> ignore ()
               logger.LogInformation <| sprintf "Received message %A" message
               return! loop() 
            }
            loop ()
            
    spawn system "command-actor" processMessage
    
let private createActorSystem () =
    System.create "system" (Configuration.defaultConfig())
    
let setupActors (smtpConfig: SmtpConfiguration) (logger: ILogger) =
   let system = createActorSystem ()
   let emailActorRef = createEmailActor smtpConfig logger system

   system.EventStream.Subscribe(emailActorRef, typedefof<Messages.Event>) |> ignore
   system
 
