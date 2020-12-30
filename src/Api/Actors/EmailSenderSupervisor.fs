module Api.Actors.EmailSenderSupervisor

open Akka.Actor
open Akka.FSharp
open FsToolkit.ErrorHandling

open Core.Domain
open Core.Domain.Types
open Core.Domain.Messages
open Api.Actors.EmailSenderActor

type GetUserInfo = Types.UserId -> Async<Result<UserInfoDto, string>>
type GetBookListingInfo = Types.ListingId -> Async<Result<BookListingInfoDto, string>>

type Dependencies = {
    GetUserData: GetUserInfo
    GetBookListingInfo: GetBookListingInfo
    SendEmail: SendEmail
}

type EmailSupervisorMessage =
    | DomainEvent of Event
    | FailedToFetchEmailInfo of originalEvent: Event * error: string
    | EmailInfoReady of EmailSenderMessage
    
let prepareBookRequestedToBorrowInfo (dependencies: Dependencies) (listingId: ListingId) (borrowerId: UserId): Async<Result<BookRequestedToBorrowInfo, string>> =
    asyncResult {
        let! borrower = dependencies.GetUserData borrowerId
        let! bookInfo = dependencies.GetBookListingInfo listingId
        let! owner = dependencies.GetUserData bookInfo.OwnerId
        return { Owner = owner; Borrower = borrower; BookInfo = bookInfo }
    }
    
let resultToEmailSupervisorMessage event toEmailReadyInfo (result: Result<'a, string>) =
    match result with
    | Error e -> FailedToFetchEmailInfo (event, e)
    | Ok info -> toEmailReadyInfo info |> EmailInfoReady
    
let emailSenderSupervisor (dependencies: Dependencies) (mailbox: Actor<EmailSupervisorMessage>) =
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
            let userIno = { Name = args.Name; Email = args.Email }
            mailbox.Self <! (SendRegistrationEmail userIno |> EmailInfoReady) 
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

let createEmailSenderSupervisor (dependencies: Dependencies) (system: ActorSystem) =
    spawn system "email-sender-supervisor" (emailSenderSupervisor dependencies)