module Api.App

open System
open Api.BookListing.SignalRHub
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.SignalR
open Fable.SignalR

open InMemoryPersistence

open Api.BookListing.RemotingHandlers

// ---------------------------------
// Web app
// ---------------------------------

// let webApp =
//     choose [
//         subRoute "/api"
//             (choose [
//                 GET >=> choose [
//                     routef "/listings/%s" BookListing.HttpHandlers.handleGetListings
//                 ]
//                 POST >=> choose [
//                     route "/users" >=> BookListing.HttpHandlers.handleCreateUser
//                 ]
//                 POST >=> choose [
//                     routef "/users/%s/listings" BookListing.HttpHandlers.handleCreateListing
//                 ]
//             ])
//         setStatusCode 404 >=> text "Not Found" ]

let webApp (root:CompositionRoot.CompositionRoot) =
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
            "*")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let compose (): CompositionRoot.CompositionRoot =
    let persistence = InMemoryPersistence.create ()
    CompositionRoot.compose persistence



let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    let root = compose ()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
            .UseCors(configureCors)
            .UseSignalR<BookListingSignalRAction, Response>(
                                                           { EndpointPattern = Endpoints.Root
                                                             Send = SignalRHubImpl.send
                                                             Invoke = SignalRHubImpl.invoke
                                                             Config = None }
                                                       )
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            //.UseHttpsRedirection()
            )
         .UseCors(configureCors)
        .UseSignalR<BookListingSignalRAction, Response>(
                                                           { EndpointPattern = Endpoints.Root
                                                             Send = SignalRHubImpl.send
                                                             Invoke = SignalRHubImpl.invoke
                                                             Config = None }
                                                       )
        .UseGiraffe(webApp root)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<CompositionRoot.CompositionRoot>(compose ()) |> ignore
    services.AddSignalR() |> ignore
    
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
