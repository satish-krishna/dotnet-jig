# dotnet-jig

Companion living repo for The .NET Web API Blueprint, a blog series that argues one decision at a time about a modern .NET 10 Web API. This repo proves each decision in code.

Series: `TODO:` (link added when the series publishes). The series is an offshoot of Jig, a Tauri plus Angular plus .NET template.

## How to read it

- `main` is the baseline: a flat, single-project users API in the out-of-the-box style. It is the "before" that every decision is diffed against.
- Each `topic/<decision>` branch applies one decision on top of the state before it. Diff a branch against its parent to see exactly what that decision changed, and what it cost.
- When a Part of the series publishes, its topic branches merge up into `main`, and `main` becomes the cumulative result. Branches are kept, never deleted, so every before/after diff stays available.

## Part I: Genesis

- `topic/composition-root` — thin composition root, self-registering layer modules.
- `topic/structure-boundaries` — real project seams, layer rules enforced by a Roslyn analyzer that fails the build on a violation.

Persistence, validation, modeled results, auth, and the rest arrive on their own branches as the series reaches them. In particular, the store here stays in memory until the Persistence decision earns its own branch.
