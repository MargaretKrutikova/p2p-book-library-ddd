namespace Core.Domain

open System

module Errors =
    type ValidationError =
        | TitleInvalid
        | AuthorInvalid
        | UserNotFound
        | ListingNotFound

    type DomainError =
        | ListingNotEligibleForOperation
        | ListingAlreadyRequestedByUser
        | BorrowErrorListingIsNotAvailable 
        | ListingIsAlreadyBorrowed
        | ListingIsNotRequested
        | ListingIsNotBorrowed
        | ListingIsAlreadyApproved

    type AppError =
        | Validation of ValidationError
        | Domain of DomainError
        | ServiceError
        static member toDomain error = Domain error |> Error
        
module Types =
    type UserId =
        private | UserId of Guid
        static member value ((UserId id)) = id
        static member create guid = UserId guid
        
    type ListingId =
        private | ListingId of Guid
        static member value ((ListingId id)) = id
        static member create guid = ListingId guid
        
    type Title = private Title of string
    type Author = private Author of string

    type ListingStatus =
        | Available
        | RequestedToBorrow of UserId
        | Borrowed of UserId
        
    // TODO: use smart constructor
    type UserName = string
    type Email = string
    
    type UserSettings = {
        IsSubscribedToUserListingActivity: bool
    }
        
    type User = {
        UserId: UserId
        Name: UserName
        Email: Email
        UserSettings: UserSettings
    }

    type BookListing =
        { ListingId: ListingId
          OwnerId: UserId
          Author: Author
          Title: Title
          Status: ListingStatus }

    module Title =
        open Errors

        let create value: Result<Title, ValidationError> =
            if String.IsNullOrWhiteSpace value
               || value.Length > 200 then
                Error TitleInvalid
            else
                value |> Title |> Ok

        let value ((Title str)) = str

    module Author =
        open Errors

        let create value: Result<Author, ValidationError> =
            if String.IsNullOrWhiteSpace value
               || value.Length > 100 then
                Error AuthorInvalid
            else
                value |> Author |> Ok

        let value ((Author str)) = str
