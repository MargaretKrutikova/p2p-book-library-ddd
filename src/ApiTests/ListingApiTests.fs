module ApiTests.ListingApiTests

open System
open Api.App
open ApiTests.Helpers
open ApiTests.Helpers.ListingApi
open FsToolkit.ErrorHandling
open Xunit

type ListingApiTests(factory: CustomWebApplicationFactory<Startup>) =
    let getUserListingById userId listingId client =
        getUsersListingsWithResponse userId client
        |> Async.map (fun userListings ->
            userListings.Listings
            |> Seq.filter (fun l -> l.ListingId = listingId)
            |> Seq.head)

    interface IClassFixture<CustomWebApplicationFactory<Startup>>

    [<Fact>]
    member __.``Publish listing returns validation errors if the user doesn't exists, author or title are invalid``() =
        let client = factory.CreateClient()

        async {
            let! publishErrorUser =
                publish
                    { UserId = Guid.NewGuid()
                      Author = "Test"
                      Title = "Test" }
                    client
                |> Utils.callWithResponse<ApiValidationErrorResponse>

            Assert.Equal("UserNotFound", publishErrorUser.Error.ValidationError)

            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client

            let! publishErrorAuthor =
                publish
                    { UserId = registeredUser.Id
                      Author = ""
                      Title = "Test" }
                    client
                |> Utils.callWithResponse<ApiValidationErrorResponse>

            Assert.Equal("AuthorInvalid", publishErrorAuthor.Error.ValidationError)

            let! publishErrorTitle =
                publish
                    { UserId = registeredUser.Id
                      Author = "Test"
                      Title = "" }
                    client
                |> Utils.callWithResponse<ApiValidationErrorResponse>

            Assert.Equal("TitleInvalid", publishErrorTitle.Error.ValidationError)
        }

    [<Fact>]
    member __.``A registered user can publish a listing and find it under all public listings``() =
        let client = factory.CreateClient()

        async {
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client

            let publishListingModel: ListingPublishInputModel =
                { UserId = registeredUser.Id
                  Author = "Adrian Tchaikovsky"
                  Title = "Children of Time" }

            let! publishedListing = publishWithResponse publishListingModel client

            let! model = getAllListingsWithResponse client

            let listingToCheck =
                model.Listings
                |> Seq.filter (fun l -> l.ListingId = publishedListing.Id)
                |> Seq.head

            Assert.Equal("Adrian Tchaikovsky", listingToCheck.Author)
            Assert.Equal("Children of Time", listingToCheck.Title)
            Assert.Equal(Available, ListingStatus.FromJson listingToCheck.Status)
        }

    [<Fact>]
    member __.``A registered user can publish a listing and see it under the user's listings``() =
        let client = factory.CreateClient()

        async {
            let! registeredUser = UserApi.registerWithResponse { Name = "test" } client

            let publishListingModel: ListingPublishInputModel =
                { UserId = registeredUser.Id
                  Author = "Adrian Tchaikovsky"
                  Title = "Children of Time" }

            let! publishedListing = publishWithResponse publishListingModel client

            let! listingToCheck = getUserListingById registeredUser.Id publishedListing.Id client

            Assert.Equal("Adrian Tchaikovsky", listingToCheck.Author)
            Assert.Equal("Children of Time", listingToCheck.Title)
            Assert.Equal(Available, ListingStatus.FromJson listingToCheck.Status)
        }

    [<Fact>]
    member __.``A registered user can request to borrow an available listing``() =
        let client = factory.CreateClient()

        async {
            let! listingOwnerUser = UserApi.registerWithResponse { Name = "owner" } client

            let publishListingModel: ListingPublishInputModel =
                { UserId = listingOwnerUser.Id
                  Author = "Adrian Tchaikovsky"
                  Title = "Children of Time" }

            let! publishedListing = publishWithResponse publishListingModel client

            let! user1 = UserApi.registerWithResponse { Name = "user1" } client

            let! user2 = UserApi.registerWithResponse { Name = "user2" } client

            // happy path
            do! requestToBorrowWithResponse { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
            let! listingToCheck = getUserListingById listingOwnerUser.Id publishedListing.Id client

            Assert.Equal("Children of Time", listingToCheck.Title)
            Assert.Equal(RequestedToBorrow { Id = user1.Id; Name = "user1" }, ListingStatus.FromJson listingToCheck.Status)

            // request again by a different user
            let! errorNotEligible =
                requestToBorrow { ListingId = publishedListing.Id; BorrowerId = user2.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>

            Assert.Equal("BorrowErrorListingIsNotAvailable", errorNotEligible.Error.DomainError)

            // request again by the borrower user
            let! errorAlreadyRequested =
                requestToBorrow { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>

            Assert.Equal("ListingAlreadyRequestedByUser", errorAlreadyRequested.Error.DomainError)

            // request by the owner
            let! errorAlreadyRequested =
                requestToBorrow { ListingId = publishedListing.Id; BorrowerId = listingOwnerUser.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>

            Assert.Equal("ListingNotEligibleForOperation", errorAlreadyRequested.Error.DomainError)
        }
    
    [<Fact>]
    member __.``A registered user can approve borrow request to his own listing``() =
        let client = factory.CreateClient()

        async {
            // Arrange
            let! listingOwnerUser = UserApi.registerWithResponse { Name = "owner" } client
            let publishListingModel: ListingPublishInputModel =
                { UserId = listingOwnerUser.Id
                  Author = "Adrian Tchaikovsky"
                  Title = "Children of Time" }
            let! publishedListing = publishWithResponse publishListingModel client
            let! user1 = UserApi.registerWithResponse { Name = "user1" } client
            
            // listing not requested
            let! errorNotRequested =
                approveBorrowRequest { ListingId = publishedListing.Id; ApproverId = listingOwnerUser.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>
            
            Assert.Equal("ListingIsNotRequested", errorNotRequested.Error.DomainError)

            // Act
            do! requestToBorrowWithResponse { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
            
            // listing can only be approved by its owner
            let! errorNotEligible =
                approveBorrowRequest { ListingId = publishedListing.Id; ApproverId = user1.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>
            
            Assert.Equal("ListingNotEligibleForOperation", errorNotEligible.Error.DomainError)

            // request is approved
            do! approveBorrowRequestWithResponse { ListingId = publishedListing.Id; ApproverId = listingOwnerUser.Id } client
            let! listingToCheck = getUserListingById listingOwnerUser.Id publishedListing.Id client

            Assert.Equal("Children of Time", listingToCheck.Title)
            Assert.Equal(Borrowed { Id = user1.Id; Name = "user1" }, ListingStatus.FromJson listingToCheck.Status)
        }
        
    [<Fact>]
    member __.``A user can returned a borrowed listing``() =
        let client = factory.CreateClient()

        async {
            // Arrange
            let! listingOwnerUser = UserApi.registerWithResponse { Name = "owner" } client
            let publishListingModel: ListingPublishInputModel =
                { UserId = listingOwnerUser.Id
                  Author = "Adrian Tchaikovsky"
                  Title = "Children of Time" }
            let! publishedListing = publishWithResponse publishListingModel client
            let! user1 = UserApi.registerWithResponse { Name = "user1" } client
            let! user2 = UserApi.registerWithResponse { Name = "user2" } client

            // listing not borrowed - available
            let! errorAvailable =
                returnListing { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>
            
            Assert.Equal("ListingIsNotBorrowed", errorAvailable.Error.DomainError)

            // listing not borrowed - requested to borrow
            do! requestToBorrowWithResponse { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
            let! errorRequested =
                returnListing { ListingId = publishedListing.Id; BorrowerId = user1.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>
            
            Assert.Equal("ListingIsNotBorrowed", errorRequested.Error.DomainError)
            
            do! approveBorrowRequestWithResponse { ListingId = publishedListing.Id; ApproverId = listingOwnerUser.Id } client
            
            // return by the wrong user
            let! errorNotEligible =
                returnListing { ListingId = publishedListing.Id; BorrowerId = user2.Id } client
                |> Utils.callWithResponse<ApiDomainErrorResponse>
                
            Assert.Equal("ListingNotEligibleForOperation", errorNotEligible.Error.DomainError)

            // listing is returned
            do! returnListingWithResponse { ListingId = publishedListing.Id; BorrowerId = user1.Id } client

            let! listingToCheck = getUserListingById listingOwnerUser.Id publishedListing.Id client

            Assert.Equal("Children of Time", listingToCheck.Title)
            Assert.Equal(Available, ListingStatus.FromJson listingToCheck.Status)
        }