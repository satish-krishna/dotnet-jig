# CLAUDE.md

Context for working in this repository, for humans and agents. The decisions behind these conventions are recorded in `docs/adr/`; read the relevant ADR before changing the shape of the code.

## What this is

A modular monolith .NET 10 Web API: one process you deploy as a whole, split into sealed modules so a piece of it can leave and run as its own service without a rewrite. It is the companion repo for the blog series "The Lazy Engineer's Blueprint."

## Layout

- `src/Jig.Host` is the composition root. It discovers modules and never learns what is inside them.
- `src/Jig.SharedKernel` holds the primitives every module may use: `Result<T>`, `Error`, `PseudoKey`, `IModule`, the integration-event contracts, and `ICurrentUser`.
- `src/Jig.Web` holds shared transport pieces: `ResultEndpoint`, the exception handler.
- `src/Modules/<Name>/Jig.Modules.<Name>` is a module. Inside it, code lives in internal folders `Domain`, `Application`, `Transport`, `Infrastructure`. A module reaches another module only through that module's `Jig.Modules.<Name>.Contracts` assembly.

## The rules (enforced, not remembered)

- Layer direction is a compiler rule. A `Transport` or `Infrastructure` type must not be referenced from `Domain`; the analyzer (rule DR0001) turns a violation into a red build. See ADR-0005 (the layered namespaces decision).
- Cross-module access is a compiler rule. A module may reference another only through its `.Contracts` (DR0004). See ADR-0001.
- Expected failures travel as `Result<T>`, not exceptions. `ResultEndpoint` maps the error kind to an HTTP status in one place. See ADR-0003.
- Persistence sits behind a domain-shaped port the module owns (for example `IUserStore`), never a generic repository, and the store is chosen at runtime from configuration. See ADR-0002 and ADR-0004.
- A module learns who is calling through `ICurrentUser` in the shared kernel, never `HttpContext`. See ADR-0005 (the ambient caller).

## Commands

- Build and test: `dotnet test` (the build runs the analyzers).
- Regenerate the typed client from the API: `bun run codegen` (fails CI if the committed client drifts from the spec).

## Skills

`.claude/skills/` holds the building-block scaffolds: `create-endpoint`, `create-domain-service`, and `create-infrastructure`. Each reads the ADRs and produces blueprint-correct code. Prefer them over hand-scaffolding a new slice.
