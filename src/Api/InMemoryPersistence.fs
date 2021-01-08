module Api.InMemoryPersistence

open System.Threading.Tasks
open Core.Domain.Types
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers
open Core.QueryModels
open FsToolkit.ErrorHandling
open System

module InMemoryPersistence =
    
    let private toBorrowerDto (user: UserDto): BorrowerDto =
        { Id = user.Id; Name = user.Name }

    let private toListingStatusDto (getUserById: UserId -> UserDto) = function 
        | ListingStatus.Available -> Available
        | ListingStatus.Borrowed id -> getUserById id |> toBorrowerDto |> Borrowed
        | ListingStatus.RequestedToBorrow id -> getUserById id |> toBorrowerDto |> RequestedToBorrow

    let private toUserBookListingDto (getUserById: UserId -> UserDto) (listing: BookListing): UserBookListingDto =
        
        { ListingId = listing.ListingId |> ListingId.value
          Author = listing.Author |> Author.value
          Title = listing.Title |> Title.value
          Status = listing.Status |> toListingStatusDto getUserById }

    let private toBookListingDto (getUserById: UserId -> UserDto) (listing: BookListing) (user: UserDto): BookListingDto =
        { ListingId = listing.ListingId |> ListingId.value
          UserName = user.Name
          UserId = user.Id
          Author = listing.Author |> Author.value
          Title = listing.Title |> Title.value
          Status = listing.Status |> toListingStatusDto getUserById }

    let create (): (CommandPersistenceOperations * QueryPersistenceOperations) =
        let mutable users: UserDto list = List.empty
        let mutable listings: BookListing list = List.empty

        let fetchUserById userId = 
            users 
            |> Seq.filter (fun user -> user.Id = (userId |> UserId.value))
            |> Seq.head

        let getListingByUserId: QueryPersistenceOperations.GetListingsByUserId =
            fun userId ->
                listings
                |> Seq.filter (fun listing -> listing.UserId = userId)
                |> Seq.map (toUserBookListingDto fetchUserById)
                |> Ok
                |> Task.FromResult

        let getUserByName: QueryPersistenceOperations.GetUserByName =
            fun userName ->
                users
                |> Seq.filter (fun user -> user.Name = userName)
                |> Seq.tryHead
                |> Result.Ok
                |> Task.FromResult

        let getUserById: CommandPersistenceOperations.GetUserById =
            fun userId ->
                users
                |> Seq.filter (fun user -> user.Id = (userId |> UserId.value))
                |> Seq.tryHead
                |> Result.requireSome CommandPersistenceOperations.MissingRecord
                |> Result.map (fun user ->
                    { Id = user.Id |> UserId.create; Name = user.Name }: CommandPersistenceOperations.UserReadModel)
                |> Task.FromResult

        let getListingById: CommandPersistenceOperations.GetListingById =
            fun listingId ->
                listings
                |> Seq.filter (fun listing -> listing.ListingId = listingId)
                |> Seq.tryHead
                |> Result.requireSome CommandPersistenceOperations.MissingRecord
                |> Result.map (fun listing ->
                    { Id = listing.ListingId
                      OwnerId = listing.UserId
                      ListingStatus = listing.Status }: CommandPersistenceOperations.ListingReadModel)
                |> Task.FromResult

        let getAllPublishedListings: QueryPersistenceOperations.GetAllPublishedListings =
            fun () ->
                listings
                |> Seq.map (fun listing ->
                    users
                    |> Seq.filter (fun u -> u.Id = (listing.UserId |> UserId.value))
                    |> Seq.head
                    |> toBookListingDto fetchUserById listing)
                |> Seq.toList
                |> Ok
                |> Task.FromResult

        let createListing: CommandPersistenceOperations.CreateListing =
            fun listing ->
                listings <- listing :: listings
                Task.FromResult(Ok())

        let createUser: CommandPersistenceOperations.CreateUser =
            fun userModel ->
                let user: UserDto =
                    { Id = userModel.UserId |> UserId.value
                      Name = userModel.Name }

                users <- user :: users
                Task.FromResult(Ok())

        let updateListingStatus: CommandPersistenceOperations.UpdateListingStatus =
            fun listingId status ->
                let updatedListings =
                    listings
                    |> Seq.map (fun l -> if l.ListingId = listingId then { l with Status = status } else l)
                    |> Seq.toList

                listings <- updatedListings
                Task.FromResult(Ok())

        let commandOperations: CommandPersistenceOperations =
            { GetUserById = getUserById
              GetListingById = getListingById
              UpdateListingStatus = updateListingStatus
              CreateListing = createListing
              CreateUser = createUser }

        let queryOperations: QueryPersistenceOperations =
            { GetUserByName = getUserByName
              GetListingsByUserId = getListingByUserId
              GetAllPublishedListings = getAllPublishedListings }

        (commandOperations, queryOperations)
