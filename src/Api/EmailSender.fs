namespace Api.Email

open System.Net.Mail
open Microsoft.Extensions.Logging
    
type SmtpConfiguration = {
    Server: string
    Sender: string
    Password: string
    PickupDirectory: string
}
    
module EmailSender =
    let private port = 587
    
    let configureSmtpClient (config: SmtpConfiguration) =
         let client = new SmtpClient(config.Server, port)
         client.DeliveryMethod <- SmtpDeliveryMethod.SpecifiedPickupDirectory
         client.PickupDirectoryLocation <- config.PickupDirectory
         client
        
    let send (config: SmtpConfiguration) (logger: ILogger) email name topic msg =
        let msg = 
            new MailMessage(config.Sender, email, topic, "Dear " + name + ", <br/><br/>\r\n\r\n" + msg)
        msg.IsBodyHtml <- true
        
        let client = configureSmtpClient config
        
        client.SendCompleted |> Observable.add(fun e -> 
            let msg = e.UserState :?> MailMessage
            if e.Cancelled then
                ("Mail message cancelled:\r\n" + msg.Subject) |> logger.LogInformation
            if e.Error <> null then
                ("Sending mail failed for message:\r\n" + msg.Subject + 
                    ", reason:\r\n" + e.Error.ToString()) |> logger.LogInformation
                
            if msg<>Unchecked.defaultof<MailMessage> then msg.Dispose()
            if client<>Unchecked.defaultof<SmtpClient> then client.Dispose()
        )
        
        client.SendAsync(msg, msg)
