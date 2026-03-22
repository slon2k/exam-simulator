# ExamSimulator

ExamSimulator is a .NET-based exam preparation platform focused on fast delivery and production-like engineering practices.

## Current baseline

- UI: Blazor Server
- Architecture: Vertical Slice with Domain Core
- Persistence: SQL Server with EF Core
- Environments: local dev, staging, prod
- Secrets: Azure Key Vault with managed identity

## Repository structure

```text
src/
  ExamSimulator.Web/          # Blazor Server app (VSA — features, domain, infrastructure as folders)
tests/
  ExamSimulator.Web.UnitTests/
  ExamSimulator.Web.FunctionalTests/
docs/
  adr/                        # Architecture Decision Records
  operations/                 # Deployment and environment setup guides
infra/
  bicep/                      # Bicep IaC (modules + environment param files)
```

## Getting started

Prerequisites:
- .NET SDK 10+

Build:

```powershell
dotnet build
```

Run web app:

```powershell
dotnet run --project src/ExamSimulator.Web/ExamSimulator.Web.csproj
```

Run tests:

```powershell
dotnet test
```

## ADRs

Architecture decisions are tracked in `docs/adr`.

## License

MIT. See LICENSE.
