# Domain

Area of knowledge for each a solution is going to modelled, resides in the problem space.

# Domain model

Representation of the domain relevant in the solution space.

# Bounded context

A subsystem in the solution space that is distinguished from the other subsystems with its own shared language, concepts and model.
A context is often mapped to a subdomain.

# Domain event

A record of something that happened in the system, triggers additional activity.
Events are fundamental for business processes.

# Command

A request for something to happen, triggered by the user or another events.
If it succeeds - state of the system changes and more domain events might be recorded.

# Value object

Domain object without identity.

# Entity

Domain object with identity that persists when properties change.

# Aggregate

Collection of related objects treated as a single component. Used as an atomic unit in db transactions.
