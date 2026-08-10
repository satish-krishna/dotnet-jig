# 3. Expected failures travel as a Result envelope

Status: accepted

## Context

Exceptions are for the unexpected. A duplicate email or a missing user is not unexpected: it is a normal outcome the caller must handle. Throwing for it turns control flow into exception handling and loses the distinction between a bug and a business rule.

## Decision

Application methods return `Result<T>`: success carries the value, failure carries a modeled `Error` with a kind (NotFound, Validation, Conflict). One place, `ResultEndpoint`, maps the error kind to an HTTP status, so no endpoint invents its own status handling. Exceptions are reserved for the genuinely unexpected and become a 500 through the exception handler.

## Consequences

Failure is modeled from day one, and the transport mapping is uniform. The cost is that every layer threads `Result` rather than returning a bare value.

Enforced by: `Result<T>` return types on the application surface and the single `ResultEndpoint` status mapping.
