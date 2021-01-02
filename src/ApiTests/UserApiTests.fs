module ApiTests.UserApiTests

open Api.App
open ApiTests.Helpers
open Xunit

type UserApiTests (factory: CustomWebApplicationFactory<Startup>) =
    interface IClassFixture<CustomWebApplicationFactory<Startup>>
  
    [<Fact>]
    member __.``A user can register and login after registering`` () =
        let client = factory.CreateClient()

        async {
            let! failedLogin = UserApi.login { Name = "test" } client |> Utils.callWithResponse<ApiSimpleErrorResponse>
            Assert.Equal(failedLogin.Error, "LoginFailure")
            
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client
            let! loggedInUser = UserApi.loginWithResponse { Name = "test" } client
            
            Assert.Equal(registeredUser.Id, loggedInUser.UserId)
        }
