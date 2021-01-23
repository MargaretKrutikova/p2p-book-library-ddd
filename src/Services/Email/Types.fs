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

type GetUserEmailInfo = UserId -> Async<Result<UserEmailInfoDto, string>>
type GetBookListingEmailInfo = ListingId -> Async<Result<BookListingEmailInfoDto, string>>

type EmailSenderDependencies = {
    GetUserEmailInfo: GetUserEmailInfo
    GetBookListingEmailInfo: GetBookListingEmailInfo
    SendEmail: SendEmail
}
