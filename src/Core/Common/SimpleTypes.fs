module Core.Common.SimpleTypes

open System

type BookListingValidationError =
  | TitleCantBeEmpty
  | TitleTooLong
  | AuthorCantBeEmpty
  | AuthorTooLong

type UserId = private UserId of Guid
type ListingId = private ListingId of Guid

type Title = string
type Author = string

type ListingStatus = Available | RequestedToBorrow | Borrowed

// ===============================
// Smart constructors
// ===============================

module UserId =
  let value ((UserId id)) = id
  let create guid = UserId guid

module ListingId =
  let value ((ListingId id)) = id
  let create guid = ListingId guid

module Title =
  let create value: Result<Title, BookListingValidationError> =
    if String.IsNullOrWhiteSpace value then
      Error TitleCantBeEmpty
    elif value.Length > 200 then
      Error TitleTooLong
    else value |> Ok

module Author =
  let create value: Result<Author, BookListingValidationError> =
    if String.IsNullOrWhiteSpace value then
      Error TitleCantBeEmpty
    elif value.Length > 100 then
      Error TitleTooLong
    else value |> Ok
