module Api.BookListing.RemotingHandlers

open System.Threading.Tasks
open Api.BookListing.Models
open Api.BookListing.ApiHandlers
open Api.CompositionRoot
open Core.BookListing.Service

open Core.Users.Service
open Microsoft.AspNetCore.Http
open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling

let private fromBookListingError (error: BookListingError): ApiError =
    match error with
    | UserDoesntExist -> UserNotFound
    | ListingDoesntExist -> ListingNotFound
    | BookListingError.ServiceError -> InternalError
    | _ -> failwith "Unknown error"

let private fromUserError (error: UserError): ApiError =
    match error with
    | UserWithProvidedNameNotFound -> UserNotFound
    | UserError.ServiceError -> InternalError

let private failOnExn (res: Result<'a, exn>) =
    match res with
    | Ok value -> value
    | Error ex -> raise ex

let private taskToApiResult toApiError task =
    task |> (AsyncResult.ofTask >> Async.map (failOnExn >> Result.mapError toApiError))

let private createUserApiFromContext (ctx:HttpContext): IUserApi = 
    let root = ctx.GetService<CompositionRoot>()
    { 
        create = createUser root >> taskToApiResult fromUserError
        login = loginUser root >> taskToApiResult fromUserError
    }

let private createBookListingApiFromContext (ctx: HttpContext): IBookListingApi =
    let root = ctx.GetService<CompositionRoot>()
    {
        getByUserId = getUserListings root >> taskToApiResult fromBookListingError
        create = createListing root >> taskToApiResult fromBookListingError
    }

let createUserApiHandler () : HttpHandler = 
    Remoting.createApi()
    |> Remoting.withRouteBuilder IUserApi.RouteBuilder
    |> Remoting.fromContext createUserApiFromContext
    |> Remoting.buildHttpHandler 

let createBookListingApiHandler () : HttpHandler = 
    Remoting.createApi()
    |> Remoting.withRouteBuilder IBookListingApi.RouteBuilder
    |> Remoting.fromContext createBookListingApiFromContext
    |> Remoting.buildHttpHandler 


module SignalRHubImpl =
    open Fable.SignalR
    open FSharp.Control.Tasks.V2
    open SignalRHub

    let update (root: CompositionRoot) (msg: BookListingSignalRAction) =
        match msg with
        | BookListingSignalRAction.CreateBookListing inputModel ->
            taskResult {
                let! _ = createListing root inputModel
                let! all = getUserListings root inputModel.UserId
                return Response.MyListings all
            } |> Task.map (Result.defaultWith (fun _ -> failwith ""))

    let invoke (msg: BookListingSignalRAction) (hubContext: FableHub) =
        // let root = hubContext.Services.GetService(typedefof<CompositionRoot>) :?> CompositionRoot
        // update root msg
        task {return Response.MyListings List.Empty } 

    // let send (msg: BookListingSignalRAction) (hubContext: FableHub) =
        //let root = hubContext.Services.GetService(typedefof<CompositionRoot>) :?> CompositionRoot
//        taskResult {
//            let! res = update root msg
//            do! (hubContext :?> FableHub<BookListingSignalRAction,Response>).Clients.Caller.Send res
//        } |> Task.map (Result.defaultWith (fun _ -> failwith "")) :> Task
       // task {return Response.MyListings List.Empty} :> Task

    let send (msg: BookListingSignalRAction) (hubContext: FableHub<BookListingSignalRAction,Response>) =
        Response.MyListings List.Empty
        |> hubContext.Clients.Caller.Send
    
//    [<RequireQualifiedAccess>]
//    module Stream =
//        let sendToClient (msg: StreamFrom.Action) (hubContext: FableHub<Action,Response>) =
//            match msg with
//            | StreamFrom.Action.AppleStocks ->
//                Stocks.appleStocks
//                |> AsyncSeq.mapAsync (fun stock ->
//                    async {
//                        do! Async.Sleep 25
//                        return StreamFrom.Response.AppleStock stock
//                    }
//                )
//                |> AsyncSeq.toAsyncEnum
        
