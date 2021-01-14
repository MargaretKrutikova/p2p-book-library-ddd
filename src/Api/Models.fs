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

type ApiError =
    | ValidationError of ValidationError
    | DomainError of DomainError
    | InternalError
    | LoginFailure

type ApiResponse<'a> = Result<'a, ApiError>

type ChangeListingStatusInputCommand =
    | RequestToBorrow
    | CancelRequestToBorrow
    | ApproveRequestToBorrow
    | ReturnListing
type ChangeListingStatusInputModel = {
    UserId: Guid
    ListingId: Guid
    Command: ChangeListingStatusInputCommand
}

type IUserApi = {
    register: UserRegisterInputModel -> Async<ApiResponse<UserRegisteredOutputModel>>
    login: UserLoginInputModel -> Async<ApiResponse<UserOutputModel>>
}
with static member RouteBuilder _ methodName = sprintf "/api/user/%s" methodName

type IBookListingApi = {
    // commands
    publish: ListingPublishInputModel -> Async<ApiResponse<ListingPublishedOutputModel>>
    changeListingStatus: ChangeListingStatusInputModel -> Async<ApiResponse<BookListingDto option>>

    // queries
    getAllListings: unit -> Async<ApiResponse<PublishedListingsOutputModel>>
    getByUserId: Guid -> Async<ApiResponse<UserListingsOutputModel>>
    getUserActivity: Guid -> Async<ApiResponse<UserActivity>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
