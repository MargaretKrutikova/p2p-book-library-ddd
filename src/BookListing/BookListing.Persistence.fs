module BookListing.Persistence

open BookListing.Domain
open System.Collections.Generic

let mutable listings: Dictionary<int, BookListing> = new Dictionary<int, BookListing>()

let getListingById (ListingId id) =
  listings.Item id

let setListingById (listing: Domain.BookListing) =
  let (ListingId id) = listing.ListingId
  listings.Item(id) <- listing 

let createListing (listing: Domain.BookListing) =
  let (ListingId id) = listing.ListingId
  listings.Add(id, listing)

