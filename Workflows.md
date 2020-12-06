# Questions

What triggers a workflow?
What is the input data?
What are the dependencies?
What is the output? (always some events that trigger actions in other bounded contexts)
What bounded contexts does it interact with?

## User registering workflow

Bounded context: user management context

Workflow: user registration

- Triggered by: "user registration form received" event
- Input:
  - primary: user registration form
  - other: email sending service
- Output events:
  - unverified user registered
- Side effects:
  - verification sent to users email

data User =
first name
last name
email
location

## Friend request workflow

Bounded context: "friend management context"

Workflow: "friend request"

- Triggered by: "friend request received" event
- Input:

  - primary:
    data UserRequest =
    primaryUserId
    userToRequestFriendshipId

  - other: email sending service

- Output events: Friend request sent
- Side effects:
  Email with a friend request sent to the friend user

## Book listing post workflow

```
bounded context: "Book listing context"

workflow: "Post a listing" =
  triggered by: "book listing form received" event
  input:
    - primary: NewBookListingForm
    - other: book catalog
  output events: BookListingPosted event or (BookListingInvalid with errors) event

  steps:

    do validateBookListing
    If invalid
      return BookListingInvalid event
      stop

    return BookListingPosted of ValidatedBookListing

substep "validateBookListing"
  input: UnvalidatedBookListingForm
  output: ValidatedBookListing OR ValidationError
  dependencies: CheckUserVerified, CheckBookExists

  ensure the user is verified
  ensure the book exists

---------------------------------------------------
data ListingIntent = Lend OR GiveAway
data VerifiedUserId = VerifiedUserId of string

data UnvalidatedBookListingForm =
  unvalidatedUserId
  title
  author
  intent

data ValidatedBookListing =
  VerifiedUserId
  VerifiedTitle
  VerifiedAuthor
  Intent
```

## Book borrowing workflow

Bounded context: "Book listing context"

Workflow: "book borrowing"

- Triggered by: "book borrowing requested" event
- Input:

  - primary:
    data BookBorrow =
    lenderUser
    borrowerUser
    bookListing

  - other: email sending service

- Output events:
  "Book borrow request accepted" event
  "Book borrow request denied" event
- Side effects:
  Send email to the lender

## Template

Bounded context: ""

Workflow: ""

- Triggered by:
- Input:
  - primary:
  - other:
- Output events:
- Side effects:
