module Api.Infrastructure.EmailSender

open System
open System.IO
open System.Threading.Tasks
open MailKit.Net.Smtp
open Microsoft.Extensions.Logging
open MimeKit
open MimeKit.IO
open MimeKit.Text
open FsToolkit.ErrorHandling.AsyncResultCE
open Services.Email.Types
 
type SmtpConfiguration = {
    SmtpServer: string
    SmtpPassword: string
    SmtpUsername: string
    Port: int
    SenderEmail: string
    SenderName: string
}
    
let private awaitEmailTask (task: Task) =
    async {
        do! task |> Async.AwaitIAsyncResult |> Async.Ignore
        if task.IsFaulted then
            return Error SendEmailError
        else
            return Ok ()
    }

let private createMessage (logger: ILogger) (config: SmtpConfiguration) (data: SendEmailData) =
    try 
        let sender = MailboxAddress.Parse(config.SenderEmail)
        sender.Name <- config.SenderEmail
        let receiver = MailboxAddress.Parse(data.Email)
        
        let message = TextPart(TextFormat.Html)
        message.Text <- data.Body
        MimeMessage([|sender :> InternetAddress|], [|receiver :> InternetAddress|], data.Topic, message) |> Ok 
    with
    | e ->
        logger.LogError(e, "Error creating message")
        Error CreateMessageError
    
let send (config: SmtpConfiguration) (logger: ILogger) (data: SendEmailData): SendEmailResult =
    asyncResult {
        let! msg =  createMessage logger config data |> async.Return 

        use smtp = new SmtpClient()
        do! smtp.ConnectAsync(config.SmtpServer, config.Port) |> awaitEmailTask
        do! smtp.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword) |> awaitEmailTask
        do! smtp.SendAsync(msg) |> awaitEmailTask
        do! smtp.DisconnectAsync(true) |> awaitEmailTask
    }
    
let sendToPickupDirectory
    (pickupDirectory: string)
    (config: SmtpConfiguration) 
    (logger: ILogger)
    (data: SendEmailData): SendEmailResult =
    asyncResult {
        let! message = createMessage logger config data |> async.Return 

        let path = Path.Combine (pickupDirectory, Guid.NewGuid().ToString() + ".eml")
        use stream = File.Open(path, FileMode.CreateNew)
        use filtered = new FilteredStream (stream)
        filtered.Add (SmtpDataFilter ())

        let options = FormatOptions.Default.Clone ()
        options.NewLineFormat <- NewLineFormat.Dos

        do! message.WriteToAsync (options, filtered) |> awaitEmailTask
        filtered.Flush ()
    }
