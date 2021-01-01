module ApiTests.UserApiTests

open System
open System.Net.Http
open System.Text
open Api.App
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Mvc.Testing
open Newtonsoft.Json
open Xunit
open Microsoft.AspNetCore.Hosting

type CustomWebApplicationFactory<'TStartup when 'TStartup : not struct> () =
    inherit WebApplicationFactory<'TStartup> ()

    override __.CreateWebHostBuilder () =
        WebHost.CreateDefaultBuilder().UseStartup<Startup>()
        
let private toStringContent (obj: obj) =
    new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")

type UserRegisteredOutputModel = {
    Id: Guid
}

type UserOutputModel = {
    UserId: Guid
    Name: string
}

type ErrorResponse = {
    Error: string
}

type UserApiTests (factory: CustomWebApplicationFactory<Startup>) =
    interface IClassFixture<CustomWebApplicationFactory<Startup>>
  
    [<Fact>]
    member __.``A user can register and login after registering`` () =
        let client = factory.CreateClient()

        async {
            let! failedLoginResponse = client.PostAsync("/api/user/login", [{| Name = "test" |}] |> toStringContent) |> Async.AwaitTask
            failedLoginResponse.EnsureSuccessStatusCode() |> ignore
            let! failedLoginResponseContent = failedLoginResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
            let failedLogin = JsonConvert.DeserializeObject<ErrorResponse>(failedLoginResponseContent) 
            
            Assert.Equal(failedLogin.Error, "LoginFailure")
            
            let! registerResponse = client.PostAsync("/api/user/register", [{| Name = "test" |}] |> toStringContent) |> Async.AwaitTask

            registerResponse.EnsureSuccessStatusCode() |> ignore
            let! registerResponseContent = registerResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
            let registeredUser = JsonConvert.DeserializeObject<UserRegisteredOutputModel>(registerResponseContent) 
            
            let! loginResponse = client.PostAsync("/api/user/login", [{| Name = "test" |}] |> toStringContent) |> Async.AwaitTask
            loginResponse.EnsureSuccessStatusCode() |> ignore

            let! loginResponseContent = loginResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
            let loggedInUser = JsonConvert.DeserializeObject<UserOutputModel>(loginResponseContent) 

            Assert.Equal(registeredUser.Id, loggedInUser.UserId)
        }

