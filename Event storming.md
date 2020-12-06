# Discover the domain

## Focus on business events rather than data

User registered
User invited

Friend request sent
Friend request accepted

Book listing added
Book lent requested
Book lent succeeded
Book returned to owner
Book given away
Book listing removal requested
Book listing removed

## Possible workflows

- part of a business process that lists the exact steps necessary to achieve its goal

User registration workflow

- save users info
- validate email
- activate account

Friend management workflow

- Send request to a friend
- Accept request on the other side

Book borrowing/lending workflow

- Add listing with intent
- Request to borrow book
- Borrow book
- Return book
- Give away book

## Commands

Commands are request for something to happen - for a workflow to start executing

A command will initiate a workflow, which will send events, which trigger commands, which initiate workflows etc.

Command -> Workflow -> Event -> Command -> Workflow

Concrete commands:

- Register user
- Connect two users as friends
- Add book listing
- Register borrow/take intent
- Return book to owner
