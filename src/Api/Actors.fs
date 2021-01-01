namespace Api.Actors

open Akka.FSharp
open Core.Domain
open Microsoft.Extensions.Logging

module ActorSystem =
    let private createActorSystem () =
        System.create "system" (Configuration.defaultConfig())
        
    let setup (emailDependencies: EmailSenderSupervisor.Dependencies) (logger: ILogger) =
       let system = createActorSystem ()
       let emailActorRef = EmailSenderSupervisor.createActor emailDependencies system

       system.EventStream.Subscribe(emailActorRef, typedefof<EmailSenderSupervisor.EmailSupervisorMessage>) |> ignore
       system
 
