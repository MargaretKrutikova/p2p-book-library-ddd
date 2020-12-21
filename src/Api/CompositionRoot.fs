module Api.CompositionRoot

open Core.BookListing.Service
open Core.Users.Service

open InMemoryPersistence

type CompositionRoot = {
  CreateListing: CreateBookListing.Composed
  RequestToBorrowBook: RequestToBorrowBook.Composed
  GetUserListings: GetUserListings.Composed
  CreateUser: CreateUser.Composed
  GetUserByName: GetUserByName.Composed
}

let compose (persistence: Persistence): CompositionRoot = 
  {
    CreateListing = CreateBookListing.execute persistence.GetUserById persistence.CreateListing
    CreateUser = CreateUser.execute persistence.CreateUser
    GetUserListings = GetUserListings.run persistence.GetUserById persistence.GetUserListings
    GetUserByName = GetUserByName.run persistence.GetUserByName  
    RequestToBorrowBook =
      RequestToBorrowBook.execute persistence.GetUserById persistence.GetListingById persistence.UpdateListing
  }
