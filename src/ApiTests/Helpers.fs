module ApiTests.Helpers

open System
open System.Net.Http
open System.Text
open Api.App
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Mvc.Testing
open Newtonsoft.Json
open Microsoft.AspNetCore.Hosting

type ApiErrorResponse = {
    Error: string
}

type CustomWebApplicationFactory<'TStartup when 'TStartup: not struct>() =
    inherit WebApplicationFactory<'TStartup>()

    override __.CreateWebHostBuilder() =
        WebHost.CreateDefaultBuilder().UseStartup<Startup>()

module Url =
    let registerUser = "/api/user/register"
    let loginUser = "/api/user/login"
    let publishListing = ""
    let getAllPublishedListings = ""

module Utils =
    let toStringContent (obj: obj) =
        new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")

    let toRemotingApiInput model = [ model ] |> toStringContent

    let callWithResponse<'a> (apiCall: Async<HttpResponseMessage>) =
        async {
            let! response = apiCall
            response.EnsureSuccessStatusCode() |> ignore

            let! content =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask

            return JsonConvert.DeserializeObject<'a>(content)
        }

module UserApi =
    type UserOutputModel = { UserId: Guid; Name: string }

    type RegisterInputModel = { Name: string }

    type LoginInputModel = { Name: string }
    
    type UserRegisteredOutputModel = {
        Id: Guid
    }
    
    let private postAsync (url: string) model (client: HttpClient) =
        client.PostAsync(url, model |> Utils.toRemotingApiInput)
        |> Async.AwaitTask

    let register (model: RegisterInputModel) (client: HttpClient) = postAsync Url.registerUser model client

    let login (model: LoginInputModel) (client: HttpClient) = postAsync Url.loginUser model client
