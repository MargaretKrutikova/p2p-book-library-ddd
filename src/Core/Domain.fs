namespace Core.Domain

open System
open FsToolkit.ErrorHandling.ResultCE

module Errors =
    type ValidationError =
        | TitleInvalid
        | AuthorInvalid
        | UserNotFound
        | BookListingNotFound

    type DomainError =
        | NotEligibleToBorrow
        | CantBorrowBeforeRequestIsApproved

    type AppError =
        | Validation of ValidationError
        | Domain of DomainError
        | ServiceError

module Types =
    type UserId = private UserId of Guid
    type ListingId = private ListingId of Guid
    type Title = private Title of string
    type Author = private Author of string

    type ListingStatus =
        | Available
        | RequestedToBorrow
        | Borrowed
    // TODO: use smart constructor
    type UserName = string
    type User = { UserId: UserId; Name: UserName }

    type BookListing =
        { ListingId: ListingId
          UserId: UserId
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

module Messages =
    open Types

    type PublishBookListingArgs =
        { NewListingId: ListingId
          UserId: UserId
          Title: string
          Author: string }

    type RequestToBorrowBookArgs =
        { ListingId: ListingId
          BorrowerId: UserId }

    type BorrowBookArgs =
        { ListingId: ListingId
          BorrowerId: UserId }

    type RegisterUserArgs = { UserId: UserId; Name: string }

    [<RequireQualifiedAccess>]
    type Command =
        | RegisterUser of RegisterUserArgs
        | PublishBookListing of PublishBookListingArgs
        | RequestToBorrowBook of RequestToBorrowBookArgs
        | BorrowBook of BorrowBookArgs

    [<RequireQualifiedAccess>]
    type Event =
        | BookListingPublished of ListingId
        | RequestedToBorrowBook of ListingId * UserId
        | BorrowedBook of ListingId * UserId
        | UserRegistered of UserId
        
    [<RequireQualifiedAccess>]
    type Query =
        | GetAllPublishedBookListings
        | GetUsersPublishedBookListings of UserId

module Logic =
    open Errors
    open Types

    let publishBookListing (dto: Messages.PublishBookListingArgs): Result<BookListing, AppError> =
        result {
            let! title =
                Title.create dto.Title
                |> Result.mapError Validation

            let! author =
                Author.create dto.Author
                |> Result.mapError Validation

            let bookListing: BookListing =
                { ListingId = dto.NewListingId
                  UserId = dto.UserId
                  Author = author
                  Title = title
                  Status = Available }

            return bookListing
        }
