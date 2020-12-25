module Api.BookListing.SignalRHub

open Api.BookListing.Models

[<RequireQualifiedAccess>]
type BookListingSignalRAction =
    | CreateBookListing of ListingCreateInputModel

[<RequireQualifiedAccess>]
type Response =
    | MyListings of ListingOutputModel list

module Endpoints =   
    let [<Literal>] Root = "/SignalR"
