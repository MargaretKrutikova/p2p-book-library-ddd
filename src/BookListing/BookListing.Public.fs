module BookListing.Public
open System.Threading.Tasks

type CreateBookListing = 
  Persistence.Queries.GetUserById 
    -> Persistence.Commands.CreateListing
    -> Domain.Commands.CreateBookListing 
    -> Task<Result<Events.BookListingEvent list, Domain.BookListingError>>
