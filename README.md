# ExamSimulator

ExamSimulator is a .NET-based exam preparation platform focused on fast delivery and production-like engineering practices.

## Current baseline

- UI: Blazor Server
- Architecture: Vertical Slice with Domain Core
- Persistence: SQL Server with EF Core (migrations applied on startup)
- Environments: local dev, staging, prod
- Secrets: Azure Key Vault with managed identity

## Features (Iteration 3)

- Admin CRUD for Questions and Exam Profiles
- Questions linked to Exam Profiles via FK (slug-based natural PK)
- Learner exam-taking flow: pick a profile → answer questions → submit → see score
- 47 unit tests + 6 functional tests, all passing

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
