module Services.Email.EmailSupervisor

open Core.Commands
open Core.Domain.Types
open Core.Events
open Services.Email.Types
open Services.Email.EmailSender

open FsToolkit.ErrorHandling

type GetUserEmailInfo = UserId -> Async<Result<UserEmailInfoDto, string>>
type GetBookListingEmailInfo = ListingId -> Async<Result<BookListingEmailInfoDto, string>>

type EmailSenderDependencies = {
    GetUserEmailInfo: GetUserEmailInfo
    GetBookListingEmailInfo: GetBookListingEmailInfo
    SendEmail: SendEmail
}

type EmailSupervisorMessage =
    | DomainEvent of Event
    | FailedToFetchEmailInfo of originalEvent: Event * error: string
    | EmailInfoReady of EmailSenderMessage

let private prepareBookRequestedToBorrowInfo (dependencies: EmailSenderDependencies) (listingId: ListingId) (borrowerId: UserId): Async<Result<BookRequestedToBorrowInfo, string>> =
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

let sendRequestToBorrowEmail (dependencies: EmailSenderDependencies) (args: ListingRequestedToBorrowEventArgs) =
    async {
        let! result = prepareBookRequestedToBorrowInfo dependencies args.ListingId args.RequesterId
        let originalEvent = Event.ListingRequestedToBorrow args
        return resultToEmailSupervisorMessage originalEvent SendBookRequestedToBorrow result
    }
    
let sendUserRegistrationEmail (dependencies: EmailSenderDependencies) (args: RegisterUserArgs) =
    let userInfo = { Name = args.Name; Email = args.Email }
    SendRegistrationEmail userInfo |> EmailInfoReady
    