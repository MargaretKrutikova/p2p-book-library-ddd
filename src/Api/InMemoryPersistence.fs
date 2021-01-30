module Api.InMemoryPersistence

open Core.Domain.Types
open Services.QueryHandlers
open Services.QueryModels
open Services.Persistence

open System.Threading.Tasks
open FsToolkit.ErrorHandling

module InMemoryPersistence =
    
    let private toBorrowerDto (user: User): BorrowerDto =
        { Id = user.UserId |> UserId.value; Name = user.Name }

    let private toUserDto (user: User): UserDto =
        { Id = user.UserId |> UserId.value
          Name = user.Name
          Email = user.Email
          IsSubscribedToUserListingActivity = user.UserSettings.IsSubscribedToUserListingActivity
        }

    let private toListingStatusDto (getUserById: UserId -> User) = function 
        | ListingStatus.Available -> Available
        | ListingStatus.Borrowed id -> getUserById id |> toBorrowerDto |> Borrowed
        | ListingStatus.RequestedToBorrow id -> getUserById id |> toBorrowerDto |> RequestedToBorrow

    let private toUserBookListingDto (getUserById: UserId -> User) (listing: BookListing): UserBookListingDto =
        
        { ListingId = listing.Id |> ListingId.value
          Author = listing.Author
          Title = listing.Title
          Status = listing.Status |> toListingStatusDto getUserById }

    let private toBookListingDto (getUserById: UserId -> User) (listing: BookListing) (user: UserDto): BookListingDto =
        { ListingId = listing.Id |> ListingId.value
          OwnerName = user.Name
          OwnerId = user.Id
          Author = listing.Author
          Title = listing.Title
          Status = listing.Status |> toListingStatusDto getUserById }

    let create (): (Commands.CommandPersistence * Common.CommonQueries * QueryPersistenceOperations) =
        let mutable users: User list = List.empty
        let mutable listings: BookListing list = List.empty

        let fetchUserById userId = 
            users 
            |> Seq.filter (fun user -> user.UserId = userId)
            |> Seq.head

        let getListingByOwnerId: QueryPersistenceOperations.GetListingsByOwnerId =
            fun userId ->
                listings
                |> Seq.filter (fun listing -> listing.OwnerId = userId)
                |> Seq.map (toUserBookListingDto fetchUserById)
                |> Ok
                |> Task.FromResult

        let getUserByName: QueryPersistenceOperations.GetUserByName =
            fun userName ->
                users
                |> Seq.filter (fun user -> user.Name = userName)
                |> Seq.tryHead
                |> Option.map toUserDto
                |> Result.Ok
                |> Task.FromResult

        let getListingByIdQuery: QueryPersistenceOperations.GetListingById =
            fun listingId ->
                listings
                |> Seq.filter (fun listing -> listing.Id = listingId)
                |> Seq.tryHead
                |> Option.map(fun listing ->
                    users
                    |> Seq.filter (fun u -> u.UserId = listing.OwnerId)
                    |> Seq.head
                    |> toUserDto
                    |> toBookListingDto fetchUserById listing)
                |> Result.Ok
                |> Task.FromResult
        
        let getUserById: Common.GetUserById =
            fun userId ->
                users
                |> Seq.filter (fun user -> user.UserId = userId)
                |> Seq.tryHead
                |> Result.requireSome MissingRecord
                |> Task.FromResult

        let getListingById: Common.GetListingById =
            fun listingId ->
                listings
                |> Seq.filter (fun listing -> listing.Id = listingId)
                |> Seq.tryHead
                |> Result.requireSome MissingRecord
                |> Task.FromResult

        let getAllPublishedListings: QueryPersistenceOperations.GetAllPublishedListings =
            fun () ->
                listings
                |> Seq.map (fun listing ->
                    users
                    |> Seq.filter (fun u -> u.UserId = listing.OwnerId)
                    |> Seq.head
                    |> toUserDto
                    |> toBookListingDto fetchUserById listing)
                |> Seq.toList
                |> Ok
                |> Task.FromResult

        let createListing: Commands.CreateListing =
            fun listing ->
                listings <- listing :: listings
                Task.FromResult(Ok())

        let createUser: Commands.CreateUser =
            fun userModel ->
                let user: User =
                    { UserId = userModel.UserId
                      Name = userModel.Name 
                      Email = userModel.Email
                      UserSettings = {
                        IsSubscribedToUserListingActivity =
                            userModel.UserSettings.IsSubscribedToUserListingActivity }
                    }
                    
                users <- user :: users
                Task.FromResult(Ok())

        let updateListingStatus: Commands.UpdateListingStatus =
            fun listingId status ->
                let updatedListings =
                    listings
                    |> Seq.map (fun l -> if l.Id = listingId then { l with Status = status } else l)
                    |> Seq.toList

                listings <- updatedListings
                Task.FromResult(Ok())

        let commandOperations: Commands.CommandPersistence =
            { UpdateListingStatus = updateListingStatus
              CreateListing = createListing
              CreateUser = createUser }
        
        let commonQueries: Common.CommonQueries =
            { GetUserById = getUserById
              GetListingById = getListingById }
            
        let queryOperations: QueryPersistenceOperations =
            { GetUserByName = getUserByName
              GetListingsByUserId = getListingByOwnerId
              GetAllPublishedListings = getAllPublishedListings
              GetListingById = getListingByIdQuery }

        (commandOperations, commonQueries, queryOperations)
