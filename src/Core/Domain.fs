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
    type UserId = private UserId of Guid
    type ListingId = private ListingId of Guid
    type Title = private Title of string
    type Author = private Author of string

    type ListingStatus =
        | Available
        | RequestedToBorrow of UserId
        | Borrowed of UserId
        
    // TODO: use smart constructor
    type UserName = string
    type User = { UserId: UserId; Name: UserName }

    type BookListing =
        { ListingId: ListingId
          OwnerId: UserId
          Author: Author
          Title: Title
          Status: ListingStatus }

    module UserId =
        let value ((UserId id)) = id
        let create guid = UserId guid

    module ListingId =
        let value ((ListingId id)) = id
        let create guid = ListingId guid

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
