module ApiTests.Helpers

open System
open System.Net.Http
open System.Text
open Api.App
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Mvc.Testing
open Newtonsoft.Json
open Microsoft.AspNetCore.Hosting

type ApiSimpleErrorResponse = {
    Error: string
}

type ApiValidationErrorResponse = {
    Error: ValidationError
} and ValidationError = {
    ValidationError: string
}

type ApiOkResponse<'a> = {
    Ok: 'a
}

module Async =
    let map f asyncValue = async {
        let! value = asyncValue
        return f value
    }

type CustomWebApplicationFactory<'TStartup when 'TStartup: not struct>() =
    inherit WebApplicationFactory<'TStartup>()

    override __.CreateWebHostBuilder() =
        WebHost.CreateDefaultBuilder().UseStartup<Startup>()

module Url =
    let registerUser = "/api/user/register"
    let loginUser = "/api/user/login"
    let publishListing = "/api/listing/publish"
    let requestToBorrowListing = "/api/listing/requestToBorrow"
    let getAllListings = "/api/listing/getAllListings"
    let getByUserId = "/api/listing/getByUserId"

module Utils =
    let toStringContent (obj: obj) =
        new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")

    let toRemotingApiInput model = [ model ] |> toStringContent
    
    let postAsync (url: string) model (client: HttpClient) =
        client.PostAsync(url, model |> toRemotingApiInput) |> Async.AwaitTask
    
    let getAsync (url: string) (client: HttpClient) =
        client.GetAsync(url) |> Async.AwaitTask

    let callWithResponse<'a> (apiCall: Async<HttpResponseMessage>) =
        async {
            let! response = apiCall
            response.EnsureSuccessStatusCode() |> ignore

            let! content =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask

            let settings = JsonSerializerSettings();
            settings.MissingMemberHandling <- MissingMemberHandling.Error;
            
            return JsonConvert.DeserializeObject<'a>(content, settings)
        }
        
    let callWithOk<'a> =
        callWithResponse<ApiOkResponse<'a>> >> Async.map (fun response -> response.Ok)

module UserApi =
    type UserOutputModel = { UserId: Guid; Name: string }
    type RegisterInputModel = { Name: string }
    type LoginInputModel = { Name: string }
    type UserRegisteredOutputModel = { Id: Guid }

    let register (model: RegisterInputModel) (client: HttpClient) =
        Utils.postAsync Url.registerUser model client
    let login (model: LoginInputModel) (client: HttpClient) =
        Utils.postAsync Url.loginUser model client

    let registerWithResponse model =
        register model >> Utils.callWithOk<UserRegisteredOutputModel>
    let loginWithResponse model = login model >> Utils.callWithOk<UserOutputModel>
    
module ListingApi =
    type ListingPublishInputModel = {
        UserId: Guid
        Author: string
        Title: string
    }
    
    type ListingPublishedOutputModel = { Id: Guid }
    type ListingStatus =
        | Available
        | RequestedToBorrow of Guid
        | Borrowed of Guid
        
    type UserListingOutputModel = {
        Id: Guid
        Author: string
        Title: string
        ListingStatus: string
    }
    type ListingOutputModel = {
        ListingId: Guid
        OwnerName: string
        Author: string
        Title: string
        ListingStatus: string
    }
    
    type PublishedListings = {
        Listings: ListingOutputModel array
    }
    type UserListingsOutputModel = {
        Listings: UserListingOutputModel list
    }
    type RequestBorrowListingInputModel = {
        BorrowerId: Guid
        ListingId: Guid 
    }
    
    let publish (model: ListingPublishInputModel) (client: HttpClient) =
        Utils.postAsync Url.publishListing model client
    
    let publishWithResponse model =
        publish model >> Utils.callWithOk<ListingPublishedOutputModel>
    
    let getAllListings client = Utils.getAsync Url.getAllListings client
    let getAllListingsWithResponse client =
        getAllListings client |> Utils.callWithOk<PublishedListings>
    
    let getUserListings (userId: Guid) client =
        Utils.postAsync Url.getByUserId userId client
    
    let getUsersListingsWithResponse userId client =
        getUserListings userId client |> Utils.callWithOk<UserListingsOutputModel>
    
    let requestToBorrow (model: RequestBorrowListingInputModel) client =
        Utils.postAsync Url.requestToBorrowListing model client
        
    let requestToBorrowWithResponse model client =
        requestToBorrow model client |> Utils.callWithOk<unit>