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
        
    type ListingStatus =
        | Available
        | RequestedToBorrow of UserId
        | Borrowed of UserId
        
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
        { Id: ListingId
          OwnerId: UserId
          Author: string
          Title: string
          Status: ListingStatus }
