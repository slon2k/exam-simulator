# ADR-0003: Cloud environments and secrets

Date: 2026-03-21
Status: Accepted

## Context
Need production-like setup without overcomplication.

## Decision
Use cloud environments staging and prod, with Key Vault for secrets and App Service managed identity for secret access.

## Consequences
- Better secret hygiene and reduced secret leakage risk.
- Clear promotion path from staging to prod.
- Slightly higher infrastructure setup than app-settings-only approach.
