namespace Api.Models
open System

[<CLIMutable>]
type UserCreatedOutputModel = {
    Id : Guid
}

[<CLIMutable>]
type ListingCreatedOutputModel = {
    Id : Guid
}

[<CLIMutable>]
type ListingOutputModel = {
    Id : Guid
    UserId : Guid
    Author : string
    Title : string
}
