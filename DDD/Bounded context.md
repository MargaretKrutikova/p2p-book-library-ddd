# Context vs domain

Problem space - domain, solution space - domain model and context. Bounded context is a mini domain model on its own.

Domains and contexts don't always have one-to-one relationship. Sometimes a domain is broken down into multiple contexts or the other way.

# Features

Context is some specialized knowledge. Each context must have a clear responsibility with a shared language spoken inside the context.

Important to get the context boundaries right. Bounded contexts have many workflows.

## Contexts in the current domain space

User context

- registration
- info etc

Friends context

- friend requests etc

Book listing context

- manage book listings
- manage book lending/borrowing process

## Interactions between the contexts - context map
