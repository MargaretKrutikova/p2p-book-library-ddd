namespace Api.BookListing.Models

open System
open Core.Domain.Errors

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

type ApiQueryError =
    | InternalError

type UserLoginError =
    | FailedToLogin

type ApiCommandResponse<'a> = Result<'a, AppError>

type IUserApi = {
    register: UserRegisterInputModel -> Async<ApiCommandResponse<UserRegisteredOutputModel>>
    login: UserLoginInputModel -> Async<Result<UserOutputModel, UserLoginError>>
}
with static member RouteBuilder _ methodName = sprintf "/api/user/%s" methodName

type IBookListingApi = {
    publish: ListingPublishInputModel -> Async<ApiCommandResponse<ListingPublishedOutputModel>>
    getByUserId: Guid -> Async<Result<UserListingOutputModel list, ApiQueryError>>
}
with static member RouteBuilder _ methodName = sprintf "/api/listing/%s" methodName
