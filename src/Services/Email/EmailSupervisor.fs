module Services.Email.EmailSupervisor

open Core.Commands
open Core.Domain.Types
open Core.Events
open Services
open Services.Email.EmailSender
open Services.Email.Types

open FsToolkit.ErrorHandling.Operator.TaskResult
open FsToolkit.ErrorHandling

type EmailSupervisorDependencies = {
    GetUserEmailInfo: Persistence.Common.GetUserById
    GetBookListingEmailInfo: Persistence.Common.GetListingById
    SendEmail: SendEmail
}

type EmailSupervisorMessage =
    | DomainEvent of EventEnvelope
    | FailedToFetchEmailInfo of originalEvent: Event * error: string
    | EmailInfoReady of EmailSenderMessage

let private errorToString = sprintf "%A"

let private prepareBookRequestedToBorrowInfo
    (dependencies: EmailSupervisorDependencies)
    (listingId: ListingId)
    (borrowerId: UserId): Async<Result<BookRequestedToBorrowInfo, string>> =
    taskResult {
        let! borrower = dependencies.GetUserEmailInfo borrowerId |> TaskResult.mapError errorToString
        let! bookInfo = dependencies.GetBookListingEmailInfo listingId |> TaskResult.mapError errorToString
        let! owner = dependencies.GetUserEmailInfo bookInfo.OwnerId |> TaskResult.mapError errorToString
        
        return { Owner = owner; Borrower = borrower; BookInfo = bookInfo }
    } |> Async.AwaitTask

let private resultToEmailSupervisorMessage event toEmailReadyInfo (result: Result<'a, string>) =
    match result with
    | Error e -> FailedToFetchEmailInfo (event, e)
    | Ok info -> toEmailReadyInfo info |> EmailInfoReady

let sendRequestToBorrowEmail (dependencies: EmailSupervisorDependencies) (args: ListingRequestedToBorrowEventArgs) =
    async {
        let! result = prepareBookRequestedToBorrowInfo dependencies args.ListingId args.RequesterId
        let originalEvent = Event.ListingRequestedToBorrow args
        return resultToEmailSupervisorMessage originalEvent SendBookRequestedToBorrow result
    }
    
let sendUserRegistrationEmail (dependencies: EmailSupervisorDependencies) (args: RegisterUserArgs) =
    let userInfo = { Name = args.Name; Email = args.Email }
    SendRegistrationEmail userInfo |> EmailInfoReady
    