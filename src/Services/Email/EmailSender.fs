module Services.Email.EmailSender

open Core.Domain.Types
open Services.Email.Types

type EmailSenderMessage =
    | SendRegistrationEmail of RegisteredUserInfo
    | SendBookRequestedToBorrow of BookRequestedToBorrowInfo
    | SendBookRequestApproved
and BookRequestedToBorrowInfo = {
    Owner: User
    Borrower: User 
    BookInfo : BookListing
}
and RegisteredUserInfo = {
    Name: string
    Email: string
}

// TODO: read from some config
let private createBookRequestedToBorrowEmailBody (info: BookRequestedToBorrowInfo) =
    sprintf "Dear %s! %s wants to borrow %s, %s. Please take action :)"
        info.Owner.Name
        info.Borrower.Name
        info.BookInfo.Title
        info.BookInfo.Author

let handleEmailSenderMessage (sendEmail: SendEmail) (message: EmailSenderMessage) =
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
