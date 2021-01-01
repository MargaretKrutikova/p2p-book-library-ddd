module Api.Actors.EmailSenderSupervisor

open Akka.Actor
open Akka.FSharp
open FsToolkit.ErrorHandling

open Core.Domain
open Core.Domain.Types
open Core.Domain.Messages
open Api.Actors.EmailSenderActor

type GetUserEmailInfo = Types.UserId -> Async<Result<UserEmailInfoDto, string>>
type GetBookListingEmailInfo = Types.ListingId -> Async<Result<BookListingEmailInfoDto, string>>

type Dependencies = {
    GetUserEmailInfo: GetUserEmailInfo
    GetBookListingEmailInfo: GetBookListingEmailInfo
    SendEmail: SendEmail
}

type EmailSupervisorMessage =
    | DomainEvent of Event
    | FailedToFetchEmailInfo of originalEvent: Event * error: string
    | EmailInfoReady of EmailSenderMessage
    
let private prepareBookRequestedToBorrowInfo (dependencies: Dependencies) (listingId: ListingId) (borrowerId: UserId): Async<Result<BookRequestedToBorrowInfo, string>> =
    asyncResult {
        let! borrower = dependencies.GetUserEmailInfo borrowerId
        let! bookInfo = dependencies.GetBookListingEmailInfo listingId
        let! owner = dependencies.GetUserEmailInfo bookInfo.OwnerId
        return { Owner = owner; Borrower = borrower; BookInfo = bookInfo }
    }
    
let private resultToEmailSupervisorMessage event toEmailReadyInfo (result: Result<'a, string>) =
    match result with
    | Error e -> FailedToFetchEmailInfo (event, e)
    | Ok info -> toEmailReadyInfo info |> EmailInfoReady
    
let private emailSenderSupervisor (dependencies: Dependencies) (mailbox: Actor<EmailSupervisorMessage>) =
    let emailActor =
        handleEmailSenderMessage dependencies.SendEmail
        |> actorOf2
        |> spawn mailbox "email-sender"
    
    let handleDomainEvent (event: Event) =
        match event with
        | Event.RequestedToBorrowBook (listingId, borrowerId) ->
            async {
                let! result = prepareBookRequestedToBorrowInfo dependencies listingId borrowerId
                return resultToEmailSupervisorMessage event SendBookRequestedToBorrow result
            } |!> mailbox.Self
        | Event.UserRegistered args ->
            let userInfo = { Name = args.Name; Email = args.Email }
            mailbox.Self <! (SendRegistrationEmail userInfo |> EmailInfoReady) 
        | _ -> ()
            
    let rec loop () = actor {
        let! message = mailbox.Receive()
        match message with
        | DomainEvent event -> handleDomainEvent event 
        | EmailInfoReady msg -> emailActor <! msg
        | FailedToFetchEmailInfo (_, e) ->
            // TODO: log error and possibly retry
            ()
        
        return! loop ()
    }
    loop ()

let createActor (dependencies: Dependencies) (system: ActorSystem) =
    spawn system "email-sender-supervisor" (emailSenderSupervisor dependencies)