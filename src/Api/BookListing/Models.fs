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

type IUserApi = {
    create: UserCreateInputModel -> Async<Result<UserCreatedOutputModel, ApiError>>
    getById: string -> Async<UserCreatedOutputModel>
}
with static member RouteBuilder _ methodName = sprintf "/api/user/%s" methodName

type IBookListingApi = {
    create: ListingCreateInputModel -> Async<ListingCreatedOutputModel>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
