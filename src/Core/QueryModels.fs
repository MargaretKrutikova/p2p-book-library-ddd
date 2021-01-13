module Core.QueryModels

open System
open System.Threading.Tasks

type QueryError =
   | InternalError   

type QueryResult<'a> = Task<Result<'a, QueryError>>

type BorrowerDto = {
    Id: Guid
    Name: string
}

type ListingStatusDto =
    | Available
    | RequestedToBorrow of BorrowerDto
    | Borrowed of BorrowerDto

type BookListingDto = {
   ListingId: Guid
   OwnerId: Guid
   OwnerName: string
   Author: string
   Title: string
   Status: ListingStatusDto
}

type UserBookListingDto = {
   ListingId: Guid
   Author: string
   Title: string
   Status: ListingStatusDto
}

type UserDto = {
    Id: Guid
    Name: string
}

type UserActivityListingStatus =
    | RequestedByUser
    | BorrowedByUser
type UserActivityListing = {
   ListingId: Guid
   OwnerId: Guid
   OwnerName: string
   Author: string
   Title: string
   Status: UserActivityListingStatus
}

type UserActivity = {
    Listings: UserActivityListing list
}