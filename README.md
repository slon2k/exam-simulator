# ExamSimulator

ExamSimulator is a .NET-based exam preparation platform.

## Stack

- **UI**: Blazor Server (.NET 10)
- **Architecture**: Vertical Slice with Domain Core
- **Persistence**: SQL Server + EF Core (migrations applied on startup)
- **Auth**: ASP.NET Core Identity (cookie auth, role-based access)
- **Environments**: local dev, staging, prod
- **Secrets**: Azure Key Vault with managed identity

## Features

- Admin CRUD for Questions and Exam Profiles (Admin role required)
- Five question types: Single Choice, Multiple Choice, Ordering, Build List, Matching
- Learner exam-taking flow: pick a profile → answer questions → submit → see score
- Authentication: register, log in, log out via built-in Identity UI
- Role-based access control: Admin pages require the `Admin` role; exam pages require any authenticated user
- Dev admin account auto-seeded on first run (Development environment only)

## Access control

| Page area | Requirement |
|---|---|
| Home (`/`) | Anonymous |
| Register / Login | Anonymous |
| Exam session (`/exams/...`) | Authenticated (any role) |
| Questions admin (`/questions/...`) | Admin role |
| Exam Profiles admin (`/exam-profiles/...`) | Admin role |

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
- SQL Server LocalDB (included with Visual Studio) or any SQL Server instance

### Local development

1. Update the connection string in `appsettings.Development.json` if needed (defaults to LocalDB).
2. Run the app — EF migrations are applied automatically on startup:

```powershell
dotnet run --project src/ExamSimulator.Web/ExamSimulator.Web.csproj
```

3. A dev admin account is seeded automatically:

| Field | Value |
|---|---|
| Email | `admin@examsimulator.local` |
| Password | `Dev@dmin1!` |

### Build and test

```powershell
dotnet build
dotnet test
```

### Granting Admin role in staging / production

Register a user through the normal Register page, then run this idempotent SQL script against the database:

```sql
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'your@email.com'
  AND r.Name = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
  );
```

## ADRs

Architecture decisions are tracked in `docs/adr`.

## License

MIT. See LICENSE.
