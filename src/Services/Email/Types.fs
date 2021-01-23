module Services.Email.Types

open Core.Domain.Types

type SendEmailData = {
    Email: string
    Topic: string
    Body: string
}
    
type EmailSenderError =
    | SendEmailError
    | CreateMessageError

type SendEmailResult = Async<Result<unit, EmailSenderError>>
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
