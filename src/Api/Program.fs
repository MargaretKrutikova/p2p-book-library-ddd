module Api.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open InMemoryPersistence

open Api.RemotingHandlers

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

let compose (): CompositionRoot.CompositionRoot =
    let persistence = InMemoryPersistence.create ()
    persistence ||> CompositionRoot.compose

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

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

type Startup() =
    member __.ConfigureServices (services : IServiceCollection) = 
        services.AddCors()    |> ignore
        services.AddGiraffe() |> ignore
        services.AddSingleton<CompositionRoot.CompositionRoot>(compose ()) |> ignore
        
    member __.Configure (app : IApplicationBuilder) (env : IHostEnvironment) (loggerFactory : ILoggerFactory) =
        configureApp app

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseStartup<Startup>()
                    |> ignore)
        .Build()
        .Run()
    0
