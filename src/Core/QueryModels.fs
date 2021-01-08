module Core.QueryModels

open System
open System.Threading.Tasks
open Core.Domain.Types

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
   UserId: Guid
   UserName: string
   Author: string
   Title: string
   Status: ListingStatus
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