module ApiTests.ListingApiTests

open System
open Api.App
open ApiTests.Helpers
open Xunit

type ListingApiTests (factory: CustomWebApplicationFactory<Startup>) =
    interface IClassFixture<CustomWebApplicationFactory<Startup>>
  
    [<Fact>]
    member __.``Publish listing returns validation errors if the user doesn't exists, author or title are invalid`` () =
        let client = factory.CreateClient()

        async {
            let! publishErrorUser =
                ListingApi.publish { UserId = Guid.NewGuid (); Author = "Test"; Title = "Test" } client
                |> Utils.callWithResponse<ApiValidationErrorResponse>
                
            Assert.Equal("UserNotFound", publishErrorUser.Error.ValidationError)
            
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client
            
            let! publishErrorAuthor =
                ListingApi.publish { UserId = registeredUser.Id; Author = ""; Title = "Test" } client
                |> Utils.callWithResponse<ApiValidationErrorResponse>
                
            Assert.Equal("AuthorInvalid", publishErrorAuthor.Error.ValidationError)
            
            let! publishErrorTitle =
                ListingApi.publish { UserId = registeredUser.Id; Author = "Test"; Title = "" } client
                |> Utils.callWithResponse<ApiValidationErrorResponse>
                
            Assert.Equal("TitleInvalid", publishErrorTitle.Error.ValidationError)
        }
    
    [<Fact>]
    member __.``A user can register, publish listings and find published listings under all listings`` () =
        let client = factory.CreateClient()

        async {
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client
            
            let publishListingModel: ListingApi.ListingPublishInputModel =
                { UserId = registeredUser.Id; Author = "Adrian Tchaikovsky"; Title = "Children of Time" }
                
            let! publishedListing = ListingApi.publishWithResponse publishListingModel client
            let! model = ListingApi.getAllListingsWithResponse client
            
            Assert.Contains(model.Listings, fun l -> l.ListingId = publishedListing.Id)
        }

