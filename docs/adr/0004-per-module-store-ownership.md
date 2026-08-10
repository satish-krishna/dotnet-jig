# 4. Each module owns its store

Status: accepted

## Context

An architecture diagram draws a clean line around the data layer and calls the store swappable. In most codebases that line is a fiction: a LINQ query leaks into a handler, a `DbContext` is injected somewhere, and swapping the store is a rewrite.

## Decision

The port belongs to the module, and the concrete store is a runtime choice read from configuration, not a compile-time reference. Because the port belongs to the module and not the application, each module owns and chooses its store independently. Users runs behind `IUserStore` with two real implementations, EF Core over SQLite and MongoDB, selected at runtime from configuration; Notifications owns its own store separately (in-memory today). Everything provider-specific stops inside the store implementation, and the two Users implementations pass one shared contract test.

## Consequences

Real store independence, and the modular monolith earns the word. The cost is two stores to keep working and the lifetime discipline (a real store is scoped) that the container validates at boot.

Enforced by: the provider-selecting factory in `UsersModule`, and the container's `ValidateScopes` and `ValidateOnBuild` at startup.
