module Api.Actors

open Akka.Actor
open Akka.FSharp
open Core.Domain

let private createEmailActor (system: ActorSystem) =
    let processMessage =
        fun (mailbox: Actor<Messages.Event>) ->
            let rec loop () = actor {
               let! message = mailbox.Receive()
               
               System.Diagnostics.Debug.WriteLine(sprintf "Received message %A" message)
               return! loop() 
            }
            loop ()
            
    spawn system "command-actor" processMessage
    
let private createActorSystem () =
    System.create "system" (Configuration.defaultConfig())
    
let setupActors () =
   let system = createActorSystem ()
   let emailActorRef = createEmailActor system

   system.EventStream.Subscribe(emailActorRef, typedefof<Messages.Event>) |> ignore
   system
 
