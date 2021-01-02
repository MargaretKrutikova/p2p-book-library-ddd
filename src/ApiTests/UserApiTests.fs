module ApiTests.UserApiTests

open Api.App
open ApiTests.Helpers
open ApiTests.ListingApiTests
open Xunit

type UserApiTests (factory: CustomWebApplicationFactory<Startup>) =
    interface IClassFixture<CustomWebApplicationFactory<Startup>>
  
    [<Fact>]
    member __.``A user can register and login after registering`` () =
        let client = factory.CreateClient()

        async {
            let! failedLogin = UserApi.login { Name = "test" } client |> Utils.callWithResponse<ErrorResponse>
            Assert.Equal(failedLogin.Error, "LoginFailure")
            
            let! registeredUser =
                UserApi.register { Name = "test" } client
                |> Utils.callWithResponse<UserApi.UserRegisteredOutputModel>
            
            let! loggedInUser =
                UserApi.login { Name = "test" } client
                |> Utils.callWithResponse<UserApi.UserOutputModel>
                
            Assert.Equal(registeredUser.Id, loggedInUser.UserId)
        }

