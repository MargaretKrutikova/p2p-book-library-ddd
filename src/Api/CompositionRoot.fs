module Api.CompositionRoot

open Core.BookListing

open InMemoryPersistence

type CompositionRoot = {
  CreateListing: Service.CreateBookListing
  RequestToBorrowBook: Service.RequestToBorrowBook
  CreateUser: Service.CreateUser
  GetUserListings: Service.GetUserListings
}

let compose (persistence: Persistence): CompositionRoot = 
  {
    CreateListing = 
      Service.createBookListing persistence.GetUserById persistence.CreateListing
    CreateUser =
      Service.createUser persistence.CreateUser
    GetUserListings = 
      Service.getUserListings persistence.GetUserById persistence.GetUserListings
    RequestToBorrowBook = 
      Service.requestToBorrowBook persistence.GetUserById persistence.GetListingById persistence.UpdateListing
  }
