# 0. Record architecture decisions

Status: accepted

## Context

The blueprint this repo implements is a sequence of deliberate decisions. Left in commit messages, a wiki, or a maintainer's head, those decisions rot: the reasoning is lost, and the next person relitigates a settled question or violates it without knowing it was ever decided.

## Decision

We record the structural decisions as ADRs in `docs/adr/`, one file per decision, in the Nygard format: context, decision, consequences, and the gate that enforces it. An ADR is not a wish. Where a decision can be machine-checked, the ADR points at the analyzer rule or the code that enforces it, so the record and the enforcement stay together.

This set is curated, not exhaustive. It captures the load-bearing architectural calls, not every small choice the series makes.

## Consequences

The decisions live next to the code and are the context both a human and an agent read before extending the system. They are one more thing to keep in step with the code, which is the price of them being true rather than aspirational.
