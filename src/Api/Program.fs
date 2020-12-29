module Api.App

open System
open Api.Email
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open InMemoryPersistence

open Api.BookListing.RemotingHandlers

// ---------------------------------
// Web app
// ---------------------------------

let webApp () =
    choose [
        createUserApiHandler ()
        createBookListingApiHandler () 
    ]
// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let compose (logger): CompositionRoot.CompositionRoot =
    let persistence = InMemoryPersistence.create ()
    
    // TODO: use ConfigurationManager.AppSettings.
    let smtpConfig: SmtpConfiguration = {
        Server = ""
        Sender = "app@app.com"
        Password = ""
        PickupDirectory = @"/"
    }
    
    persistence ||> CompositionRoot.compose smtpConfig logger

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            //.UseHttpsRedirection()
            )
        .UseCors(configureCors)
        .UseGiraffe(webApp ())

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<CompositionRoot.CompositionRoot>
        (fun container ->
            let logger = container.GetRequiredService<ILogger<IStartup>>() // TODO: fix later
            compose logger) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0
