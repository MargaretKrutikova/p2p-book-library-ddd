module Api.CompositionRoot

open Core.BookListing.Service

open InMemoryPersistence

type CompositionRoot = {
  CreateListing: CreateBookListing.Composed
  RequestToBorrowBook: RequestToBorrowBook.Composed
  CreateUser: CreateUser.Composed
  GetUserListings: GetUserListings.Composed
}

let compose (persistence: Persistence): CompositionRoot = 
  {
    CreateListing = CreateBookListing.execute persistence.GetUserById persistence.CreateListing
    CreateUser = CreateUser.execute persistence.CreateUser
    GetUserListings = GetUserListings.run persistence.GetUserById persistence.GetUserListings
    RequestToBorrowBook =
      RequestToBorrowBook.execute persistence.GetUserById persistence.GetListingById persistence.UpdateListing
  }
