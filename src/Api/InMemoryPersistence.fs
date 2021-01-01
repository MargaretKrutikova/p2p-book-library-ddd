module Api.InMemoryPersistence

open System.Threading.Tasks
open Api.Actors.EmailSenderActor
open Api.Actors.EmailSenderSupervisor
open Core.Domain.Types
open Core.Handlers.CommandHandlers
open Core.Handlers.QueryHandlers
open FsToolkit.ErrorHandling

type InfrastructurePersistenceOperations =
    { GetUserEmailInfo: GetUserEmailInfo
      GetBookListingEmailInfo: GetBookListingEmailInfo }

module InMemoryPersistence =
    let private toUserBookListingDto (listing: BookListing): UserBookListingDto =
        { ListingId = listing.ListingId
          Author = listing.Author |> Author.value
          Title = listing.Title |> Title.value
          Status = listing.Status }

    let create (): (CommandPersistenceOperations * QueryPersistenceOperations * InfrastructurePersistenceOperations) =
        let mutable users: UserDto list = List.empty
        let mutable listings: BookListing list = List.empty

        let getListingByUserId: QueryPersistenceOperations.GetListingsByUserId =
            fun userId ->
                listings
                |> Seq.filter (fun listing -> listing.UserId = userId)
                |> Seq.map toUserBookListingDto
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
                |> Seq.filter (fun user -> user.Id = userId)
                |> Seq.tryHead
                |> Result.requireSome CommandPersistenceOperations.MissingRecord
                |> Result.map (fun user ->
                    { Id = user.Id; Name = user.Name }: CommandPersistenceOperations.UserReadModel)
                |> Task.FromResult

        let createListing: CommandPersistenceOperations.CreateListing =
            fun listing ->
                listings <- listing :: listings
                Task.FromResult(Ok())

        let createUser: CommandPersistenceOperations.CreateUser =
            fun userModel ->
                let user: UserDto =
                    { Id = userModel.UserId
                      Name = userModel.Name
                      Email = userModel.Email
                      IsSubscribedToUserListingActivity = userModel.UserSettings.IsSubscribedToUserListingActivity }

                users <- user :: users
                Task.FromResult(Ok())

        let updateListing: CommandPersistenceOperations.UpdateListingStatus =
            fun listingId status ->
                let updatedListings =
                    listings
                    |> Seq.map (fun l -> if l.ListingId = listingId then { l with Status = status } else l)
                    |> Seq.toList

                listings <- updatedListings
                Task.FromResult(Ok())

        let getUserEmailInfo: GetUserEmailInfo =
            fun userId ->
                users
                |> Seq.filter (fun user -> user.Id = userId)
                |> Seq.tryHead
                |> Result.requireSome "User not found"
                |> Result.map (fun user ->
                    { Name = user.Name
                      Email = user.Email
                      IsSubscribedToUserListingActivity = user.IsSubscribedToUserListingActivity }: UserEmailInfoDto)
                |> async.Return

        let getBookListingEmailInfo: GetBookListingEmailInfo =
            fun listingId ->
                listings
                |> Seq.filter (fun listing -> listing.ListingId = listingId)
                |> Seq.tryHead
                |> Result.requireSome "Book listing not found"
                |> Result.map (fun listing ->
                    { OwnerId = listing.UserId
                      Title = listing.Title |> Title.value
                      Author = listing.Author |> Author.value }: BookListingEmailInfoDto)
                |> async.Return

        let commandOperations: CommandPersistenceOperations =
            { GetUserById = getUserById
              CreateListing = createListing
              CreateUser = createUser }

        let queryOperations: QueryPersistenceOperations =
            { GetUserByName = getUserByName
              GetListingsByUserId = getListingByUserId }

        let infrastructureOperations: InfrastructurePersistenceOperations =
            { GetUserEmailInfo = getUserEmailInfo
              GetBookListingEmailInfo = getBookListingEmailInfo }

        (commandOperations, queryOperations, infrastructureOperations)
