namespace Api.BookListing.Models

open System
open Core.BookListing.Service

[<CLIMutable>]
type UserCreateInputModel = {
    Name: string
}

[<CLIMutable>]
type UserCreatedOutputModel = {
    Id: Guid
}

[<CLIMutable>]
type ListingCreateInputModel = {
    UserId: Guid
    Author: string
    Title: string
}

[<CLIMutable>]
type ListingCreatedOutputModel = {
    Id: Guid
}

[<CLIMutable>]
type ListingOutputModel = {
    Id: Guid
    UserId: Guid
    Author: string
    Title: string
}

type ApiError = 
    | UserNotFound
    | ListingNotFound
    | InternalError

type ApiResponse<'a> = Result<'a, ApiError>

type IUserApi = {
    create: UserCreateInputModel -> Async<ApiResponse<UserCreatedOutputModel>>
    getById: Guid -> Async<ApiResponse<UserCreatedOutputModel>>
}
with static member RouteBuilder _ methodName = sprintf "/api/user/%s" methodName

type IBookListingApi = {
    create: ListingCreateInputModel -> Async<ApiResponse<ListingCreatedOutputModel>>
    getByUserId: Guid -> Async<ApiResponse<ListingOutputModel list>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
