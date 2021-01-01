module Api.App

open System
open System.Configuration
open Api.Email
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
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

let compose (configuration: IConfiguration) (logger): CompositionRoot.CompositionRoot =
    let persistence = InMemoryPersistence.create ()
    
    let smtpConfigJson = configuration.GetSection("SmtpConfiguration")
    let smtpConfig: SmtpConfiguration = {
        SmtpServer = smtpConfigJson.GetValue("SmtpServer")
        SmtpPassword = smtpConfigJson.GetValue("SmtpPassword")
        SmtpUsername = smtpConfigJson.GetValue("SmtpUsername")
        SenderEmail = smtpConfigJson.GetValue("SenderEmail")
        SenderName = smtpConfigJson.GetValue("SenderName")
        Port = smtpConfigJson.GetValue("Port") }
    
    let pickupDirectory = @""

    persistence |||> CompositionRoot.compose smtpConfig pickupDirectory logger

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
    let serviceProvider = services.BuildServiceProvider()
    let config = serviceProvider.GetService<IConfiguration>()
    
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<CompositionRoot.CompositionRoot>
        (fun container ->
            let logger = container.GetRequiredService<ILogger<IStartup>>() // TODO: fix later
            compose config logger) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

let configureAppConfiguration  (context: WebHostBuilderContext) (config: IConfigurationBuilder) =  
    config
        .AddJsonFile("appsettings.json",false,true)
        .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName ,true)
        .AddEnvironmentVariables() |> ignore

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureAppConfiguration(configureAppConfiguration)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0
