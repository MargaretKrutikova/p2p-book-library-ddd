module Api.Actors.EmailSenderActor

open Akka.FSharp
open Api.Email
open Core.Domain.Types

type SendEmail = SendEmailData -> Async<Result<unit, string>>

type UserInfoDto = {
    Name: string
    Email: string
    IsSubscribedToUserListingActivity: bool
}

type BookListingInfoDto = {
    OwnerId: UserId
    Title: string
    Author: string
}

type EmailSenderMessage =
    | SendRegistrationEmail of RegisteredUserInfo
    | SendBookRequestedToBorrow of BookRequestedToBorrowInfo
    | SendBookRequestApproved
and BookRequestedToBorrowInfo = {
    Owner: UserInfoDto
    Borrower: UserInfoDto 
    BookInfo : BookListingInfoDto
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
       sendEmail data |> ignore // TODO: handle
   | SendBookRequestedToBorrow info ->
       let data = {
           Email = info.Owner.Email
           Topic = "Borrow book request"
           Body = createBookRequestedToBorrowEmailBody info
       }
       sendEmail data |> ignore
   | _ -> ()
