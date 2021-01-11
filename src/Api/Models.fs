namespace Api.Models

open System
open Core.Domain.Errors
open Core.QueryModels

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
type PublishedListingsOutputModel = {
    Listings: BookListingDto list
}

[<CLIMutable>]
type UserListingsOutputModel = {
    Listings: UserBookListingDto list
}

[<CLIMutable>]
type RequestBorrowListingInputModel = {
    BorrowerId: Guid
    ListingId: Guid 
}

[<CLIMutable>]
type ApproveBorrowRequestInputModel = {
    OwnerId: Guid
    ListingId: Guid 
}

[<CLIMutable>]
type ReturnListingInputModel = {
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
    // commands
    publish: ListingPublishInputModel -> Async<ApiResponse<ListingPublishedOutputModel>>
    requestToBorrow: RequestBorrowListingInputModel -> Async<ApiResponse<unit>>
    approveBorrowRequest: ApproveBorrowRequestInputModel -> Async<ApiResponse<unit>>
    returnListing: ReturnListingInputModel -> Async<ApiResponse<unit>>

    // queries
    getAllListings: unit -> Async<ApiResponse<PublishedListingsOutputModel>>
    getByUserId: Guid -> Async<ApiResponse<UserListingsOutputModel>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
