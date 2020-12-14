module Core.BookListing.Implementation

open Core.BookListing.Domain
open Core.Common.SimpleTypes

open FsToolkit.ErrorHandling

let createBookListing (dto: CreateBookListingDto): Result<Domain.BookListing, BookListingDomainError> =
    result {
      let! title = Title.create dto.Title |> Result.mapError ValidationError
      let! author = Author.create dto.Author |> Result.mapError ValidationError

      let bookListing: Domain.BookListing = {
        ListingId = dto.NewListingId
        UserId = dto.UserId
        Author = author
        Title = title
        Status = Available
      }

      return bookListing
    }
      
let requestToBorrow (status: ListingStatus): Result<ListingStatus, BookListingDomainError> =
    result {
      match status with
      | Available -> return! Ok RequestedToBorrow
      | _ -> return! Error NotEligibleForBorrow
    }
