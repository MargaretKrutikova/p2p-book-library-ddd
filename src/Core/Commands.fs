module Core.Commands

open Core.Domain.Types

type PublishBookListingArgs =
    { NewListingId: ListingId
      UserId: UserId
      Title: string
      Author: string }

type RegisterUserArgs = { UserId: UserId; Name: string }

type ChangeListingStatusCommand =
    | RequestToBorrow
    | CancelRequestToBorrow
    | ApproveRequestToBorrow
    | ReturnBorrowedListing

type ChangeListingStatusArgs =
    { ChangeRequestedByUserId: UserId
      ListingId: ListingId
      Command: ChangeListingStatusCommand }

[<RequireQualifiedAccess>]
type Command =
    | RegisterUser of RegisterUserArgs
    | PublishBookListing of PublishBookListingArgs
    | ChangeListingStatus of ChangeListingStatusArgs
