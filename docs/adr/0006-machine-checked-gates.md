# 6. Rules are machine-checked, not remembered

Status: accepted

## Context

A rule that lives only in a document is a rule nobody enforces, and it has already started to rot. "Please remember to keep Domain free of Infrastructure" is how the inconsistency got in.

## Decision

Every rule that can be enforced by the compiler, a generator, or a gate is, rather than by a note that says please remember. Layer and cross-module rules are Roslyn analyzers that turn a violation into a red build. The wire contracts a client depends on are generated from the OpenAPI spec, so the API and its callers cannot silently drift apart. CI runs the build (and therefore the analyzers), the tests, and the codegen drift check on every push, so a violation refuses the merge rather than relying on a reviewer noticing.

## Consequences

The rules survive without anyone remembering them. The cost is gate maintenance and the occasional false red.

Enforced by: `Jig.Analyzers` (DR0001 through DR0004), the generated TypeScript client and its drift check, and the CI workflow.
