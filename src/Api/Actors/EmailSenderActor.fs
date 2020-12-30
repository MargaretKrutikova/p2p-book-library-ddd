module Api.Actors.EmailSenderActor

open Akka.FSharp
open Core.Domain.Types

type SendEmailData = {
    Email: string
    Topic: string
    Body: string
}

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

let handleEmailSenderMessage (sendEmail: SendEmail) (_: Actor<EmailSenderMessage>) (message: EmailSenderMessage) =
   match message with
   | SendRegistrationEmail info ->
       let data = {
           Email = info.Email
           Topic = "Welcome"
           Body = "Tja"
       }
       sendEmail data |> ignore // TODO: handle
   | _ -> ()
