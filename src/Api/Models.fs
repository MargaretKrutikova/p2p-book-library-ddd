namespace Api.Models

open System
open Core.Domain.Errors
open Core.Domain.Types

[<CLIMutable>]
type UserRegisterInputModel = {
    Name: string
}

[<CLIMutable>]
type UserRegisteredOutputModel = {
    Id: Guid
}

[<CLIMutable>]
type UserLoginInputModel = {
    Name: string
}

[<CLIMutable>]
type UserOutputModel = {
    UserId: Guid
    Name: string
}

[<CLIMutable>]
type ListingPublishInputModel = {
    UserId: Guid
    Author: string
    Title: string
}

[<CLIMutable>]
type ListingPublishedOutputModel = {
    Id: Guid
}

[<CLIMutable>]
type UserListingOutputModel = {
    Id: Guid
    Author: string
    Title: string
}

[<CLIMutable>]
type ListingOutputModel = {
    ListingId: Guid
    OwnerName: string
    Author: string
    Title: string
    ListingStatus: ListingStatus
}

[<CLIMutable>]
type PublishedListingsOutputModel = {
    Listings: ListingOutputModel list
}

[<CLIMutable>]
type UserListingsOutputModel = {
    Listings: UserListingOutputModel list
}

[<CLIMutable>]
type RequestBorrowListingInputModel = {
    BorrowerId: Guid
    ListingId: Guid 
}

type ApiError =
    | ValidationError of ValidationError
    | DomainError of DomainError
    | InternalError
    | LoginFailure

type ApiResponse<'a> = Result<'a, ApiError>

type IUserApi = {
    register: UserRegisterInputModel -> Async<ApiResponse<UserRegisteredOutputModel>>
    login: UserLoginInputModel -> Async<ApiResponse<UserOutputModel>>
}
with static member RouteBuilder _ methodName = sprintf "/api/user/%s" methodName

type IBookListingApi = {
    publish: ListingPublishInputModel -> Async<ApiResponse<ListingPublishedOutputModel>>
    requestBorrowListing: RequestBorrowListingInputModel -> Async<ApiResponse<unit>>
    getAllListings: unit -> Async<ApiResponse<PublishedListingsOutputModel>>
    getByUserId: Guid -> Async<ApiResponse<UserListingsOutputModel>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
