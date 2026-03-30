# ADR-0001: Architecture style

Date: 2026-03-21
Status: Accepted

## Context

Need fast solo delivery while keeping domain rules maintainable.

## Decision

Use Vertical Slice Architecture with a lightweight Domain Core.

## Consequences

- Faster feature delivery per use case.
- Business rules stay explicit in domain entities/services.
- Keep shared abstractions minimal and extract only when needed.
