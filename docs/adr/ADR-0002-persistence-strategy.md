# ADR-0002: Persistence strategy

Date: 2026-03-21
Status: Accepted

## Context
Need relational consistency and flexible querying for exams, questions, attempts, and review screens.

## Decision
Use SQL Server with EF Core as primary persistence.

## Consequences
- Strong transactional consistency and schema constraints.
- Straightforward query model for admin and learner workflows.
- Lower complexity than NoSQL modeling for current workload.
