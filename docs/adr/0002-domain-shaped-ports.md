# 2. Domain-shaped ports, not a generic repository

Status: accepted

## Context

A generic `IRepository<T>` is the lowest common denominator of every store. It gives you `Get` and `List` and forbids you the join or the aggregation you chose your database for, so you either leak `IQueryable` (un-abstracting the store) or bolt store-specific methods onto the "generic" interface until it is not generic.

## Decision

Each module defines a port shaped by what it actually does with its data, for example `IUserStore` with the four methods the module needs, not a generic repository. The port and the identity it stores (a domain-minted `PseudoKey`, not a store-assigned id) belong to the module, which is what lets a module choose its own store.

## Consequences

More interfaces than one generic repository, and on a trivial CRUD table the generic would have been less code. The bet is that you chose a real database for a reason and will want to use it.

Enforced by: convention and code review; the ports live in each module's domain, and no `IRepository` exists in the tree.
