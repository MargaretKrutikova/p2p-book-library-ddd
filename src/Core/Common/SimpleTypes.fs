module Core.Common.SimpleTypes
open System

type UserId = private UserId of Guid

module UserId =
  let value ((UserId id)) = id
  let create guid = UserId guid

type ListingId = private ListingId of Guid

module ListingId =
  let value ((ListingId id)) = id
  let create guid = ListingId guid

