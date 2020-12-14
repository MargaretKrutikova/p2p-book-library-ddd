module Core.BookListing.Domain

open Core.Common.SimpleTypes
open System

type BookListingDomainError =
  | ValidationError of BookListingValidationError
  | NotEligibleForBorrow

type BookListing = {
  ListingId: ListingId
  UserId: UserId
  Author: Author
  Title: Title
  Status: ListingStatus
}

type CreateBookListingDto = {
  NewListingId: ListingId
  UserId: UserId
  Title: string
  Author: string
}
