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
    member __.``A registered user can publish a listing and find it under all public listings`` () =
        let client = factory.CreateClient()

        async {
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client
            
            let publishListingModel: ListingApi.ListingPublishInputModel =
                { UserId = registeredUser.Id; Author = "Adrian Tchaikovsky"; Title = "Children of Time" }
                
            let! publishedListing = ListingApi.publishWithResponse publishListingModel client
            let! model = ListingApi.getAllListingsWithResponse client
            
            let listingToCheck =
                model.Listings
                |> Seq.filter (fun l -> l.ListingId = publishedListing.Id)
                |> Seq.head
            
            Assert.Equal("Adrian Tchaikovsky", listingToCheck.Author)
            Assert.Equal("Children of Time", listingToCheck.Title)
        }
    
    [<Fact>]
    member __.``A registered user can publish a listing and see it under the user's listings`` () =
        let client = factory.CreateClient()

        async {
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client
            
            let publishListingModel: ListingApi.ListingPublishInputModel =
                { UserId = registeredUser.Id; Author = "Adrian Tchaikovsky"; Title = "Children of Time" }
                
            let! publishedListing = ListingApi.publishWithResponse publishListingModel client
            let! model = ListingApi.getUsersListingsWithResponse registeredUser.Id client
            
            let listingToCheck =
                model.Listings
                |> Seq.filter (fun l -> l.Id = publishedListing.Id)
                |> Seq.head
            
            Assert.Equal("Adrian Tchaikovsky", listingToCheck.Author)
            Assert.Equal("Children of Time", listingToCheck.Title)
            // TODO: check correct status when implemented
        }
