# 1. A modular monolith with sealed boundaries

Status: accepted

## Context

One process is the cheapest thing to deploy and operate. A big ball of mud in one process is the most expensive thing to change. We want the deployment simplicity of a monolith without the coupling that makes a monolith unchangeable.

## Decision

The application is a modular monolith: one process, split into sealed module assemblies under `src/Modules/<Name>/`. A module owns its slice top to bottom (its `Domain`, `Application`, `Transport`, `Infrastructure`), and it may reach another module only through that module's published `.Contracts` assembly, never its internals. The seam is drawn so the day a module has to leave and become its own service is a bounded change, not a rewrite.

## Consequences

Cheap extraction, not free deployment composition. The cost is more projects and the discipline of never reaching past a contract.

Enforced by: the `Jig.Analyzers` rule DR0004 turns a cross-module reference that is not through `.Contracts` into a compile error.
