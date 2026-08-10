# 5. The caller reaches a module as an ambient abstraction

Status: accepted

## Context

Authorization and auditing need to know who is calling. A module that reads `HttpContext.User` to find out is coupled to the web host, which breaks the boundary the analyzer enforces and makes the module impossible to extract or test in isolation.

## Decision

The caller is exposed to modules as `ICurrentUser` in `Jig.SharedKernel`: a small shape (is-authenticated, user id, scopes), deliberately not the `ClaimsPrincipal`. The host owns the single implementation that reads `IHttpContextAccessor` and maps claims into that shape. A module injects `ICurrentUser` and never learns the identity came from HTTP, so identity crosses into a module without a module-to-host dependency.

## Consequences

The abstraction sits in the shared kernel and the HTTP-coupled implementation sits in the host, so there is no cycle. Resource-ownership rules that cannot be a static endpoint policy are expressed in the module through this abstraction. The cost is a projection to keep in step with the claims the identity provider actually issues.

Enforced by: `ICurrentUser` in `Jig.SharedKernel`, its implementation only in the host, and no module referencing the host.
