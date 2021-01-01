module Api.Actors.EmailSenderActor

open Akka.FSharp
open Api.Email
open Core.Domain.Types

type SendEmail = SendEmailData -> SendEmailResult

type UserEmailInfoDto = {
    Name: string
    Email: string
    IsSubscribedToUserListingActivity: bool
}

type BookListingEmailInfoDto = {
    OwnerId: UserId
    Title: string
    Author: string
}

type EmailSenderMessage =
    | SendRegistrationEmail of RegisteredUserInfo
    | SendBookRequestedToBorrow of BookRequestedToBorrowInfo
    | SendBookRequestApproved
and BookRequestedToBorrowInfo = {
    Owner: UserEmailInfoDto
    Borrower: UserEmailInfoDto 
    BookInfo : BookListingEmailInfoDto
}
and RegisteredUserInfo = {
    Name: string
    Email: string
}

// TODO: read from some config
let createBookRequestedToBorrowEmailBody (info: BookRequestedToBorrowInfo) =
    sprintf "Dear %s! %s wants to borrow %s, %s. Please take action :)"
        info.Owner.Name
        info.Borrower.Name
        info.BookInfo.Title
        info.BookInfo.Author

let handleEmailSenderMessage (sendEmail: SendEmail) (_: Actor<EmailSenderMessage>) (message: EmailSenderMessage) =
   match message with
   | SendRegistrationEmail info ->
       let data = {
           Email = info.Email
           Topic = "Welcome"
           Body = "Tja"
       }
       sendEmail data |> Async.StartAsTask |> ignore
   | SendBookRequestedToBorrow info ->
       let data = {
           Email = info.Owner.Email
           Topic = "Borrow book request"
           Body = createBookRequestedToBorrowEmailBody info
       }
       sendEmail data |> Async.StartAsTask |> ignore
   | _ -> ()
