module Core.Commands

open Core.Domain.Types

type PublishBookListingArgs =
    { UserId: UserId
      Title: string
      Author: string }

type ChangeListingStatusCommand =
    | RequestToBorrow
    | CancelRequestToBorrow
    | ApproveRequestToBorrow
    | ReturnBorrowedListing

type ChangeListingStatusArgs =
    { ChangeRequestedByUserId: UserId
      Command: ChangeListingStatusCommand }

[<RequireQualifiedAccess>]
type Command =
    | PublishBookListing of PublishBookListingArgs
    | ChangeListingStatus of ChangeListingStatusArgs
