namespace Api.Models

open System
open Core.Domain.Errors

[<CLIMutable>]
type UserRegisterInputModel = {
    Name: string
    Email: string
    IsSubscribedToUserListingActivity: bool
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
    getByUserId: Guid -> Async<ApiResponse<UserListingOutputModel list>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
