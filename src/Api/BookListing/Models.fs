namespace Api.BookListing.Models

open System

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
