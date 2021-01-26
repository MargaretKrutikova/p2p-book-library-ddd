namespace Api.Actors

open Akka.FSharp
open Microsoft.Extensions.Logging

open Services.Email.EmailSupervisor

module ActorSystem =
    let private createActorSystem () =
        System.create "system" (Configuration.defaultConfig())
        
    let setup (emailDependencies: EmailSupervisorDependencies) (logger: ILogger) =
       let system = createActorSystem ()
       let emailActorRef = EmailSenderSupervisor.createActor emailDependencies system

       system.EventStream.Subscribe(emailActorRef, typedefof<EmailSupervisorMessage>) |> ignore
       system
 
