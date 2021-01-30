module Services.Email.Types

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
