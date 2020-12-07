module BookListing.Persistence
open Domain.Events

let mutable listings: Domain.BookListing list = List.empty<Domain.BookListing>

let handleEvent (event: BookListingEvent) =
  match event with
  | BookBorrowed data -> 
    listings <- listings 
  | BookListingCreated data -> 
    listings <- data.Listing :: listings

module Queries =
  let getListings () = listings
