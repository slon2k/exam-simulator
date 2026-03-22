# Iteration 2 Plan: Infrastructure Baseline + Question Model Completion

## Current State (End of Iteration 1)

- VSA single-project structure in place. ✅
- `Question` domain entity with invariants. ✅
- EF Core + SQLite dev setup with `InitialCreate` migration. ✅
- `CreateQuestion` and `ListQuestions` Blazor pages. ✅
- 13 domain unit tests + 2 functional tests, all passing. ✅
- Infra bicep folders present but empty. ❌
- No CD workflow. ❌
- No Edit/Delete for questions. ❌
- `Question` has no type, difficulty, or explanation fields. ❌
- Multiple-choice support is absent (single correct index only). ❌

## Goals

1. Lay the infrastructure and deployment baseline deferred from Iteration 1.
2. Expand the `Question` domain model to support the question types required by MVP (single-choice and multiple-choice).
3. Complete admin CRUD for questions (Edit + Delete).

## Key Decisions

- **Question type modelling**: Unify correct answers as `IReadOnlyList<int> CorrectOptionIndices`. For `SingleChoice` the invariant enforces exactly one index; for `MultipleChoice` one or more. Ordering questions are deferred to Iteration 3.
- **Difficulty**: Introduce `Difficulty` enum (`Easy`, `Medium`, `Hard`) on `Question`. Required.
- **Explanation**: Add nullable `Explanation` string to `Question`. Content is plain text for now; Markdown rendering deferred.
- **SQL Server for cloud**: Add `Microsoft.EntityFrameworkCore.SqlServer` package and select the provider at startup based on environment. SQLite stays for local dev; SQL Server is used in staging and prod.
- **IaC**: Bicep modules for App Service, Azure SQL, and Key Vault; environment entry points for staging and prod.
- **CD**: A single manually-triggered GitHub Actions workflow deploying to staging.

## Target State (End of Iteration 2)

```
infra/
  bicep/
    main.bicep             ← single entry point for all environments
    modules/
      appservice.bicep
      sql.bicep
      keyvault.bicep
    environments/
      staging/
        staging.bicepparam
      prod/
        prod.bicepparam
.github/
  workflows/
    deploy-staging.yml
src/
  ExamSimulator.Web/
    Domain/
      Questions/
        Question.cs          ← adds Type, Difficulty, Explanation, CorrectOptionIndices
        QuestionType.cs      ← new enum
        Difficulty.cs        ← new enum
    Features/
      Questions/
        CreateQuestion.razor ← updated for new fields and types
        ListQuestions.razor  ← updated columns + Edit/Delete links
        EditQuestion.razor   ← new
        DeleteQuestion.razor ← new
    Infrastructure/
      Migrations/
        ..._AddTypeAndDifficulty.cs  ← new migration
```

## Phases

---

### Phase 1: Infrastructure IaC Skeleton

**Closes:** #5

Steps:

1. Create `infra/bicep/modules/appservice.bicep` — parameterised App Service Plan + Web App (Linux, .NET 10, Standard S1).
2. Create `infra/bicep/modules/sql.bicep` — Azure SQL Server + Database; admin credentials via Key Vault reference.
3. Create `infra/bicep/modules/keyvault.bicep` — Key Vault; grants App Service managed identity `Key Vault Secrets User` role.
4. Create `infra/bicep/main.bicep` — single entry point that wires the three modules. Accepts parameters for environment name, SKUs, and resource names so the same file deploys to any environment.
5. Create `infra/bicep/environments/staging/staging.bicepparam` — staging-specific parameter values (smaller SKUs, staging resource names).
6. Create `infra/bicep/environments/prod/prod.bicepparam` — prod-specific parameter values.
7. Document environment variable wiring (connection string pulled from Key Vault via App Service Key Vault reference) in `docs/operations/infra-overview.md`.

**Verification:**

- `az bicep build` on `main.bicep` succeeds with no errors.
- Linter passes with no warnings.
- Both `.bicepparam` files resolve correctly against `main.bicep`.

**Commit:** `feat: Bicep IaC skeleton for App Service, SQL, and Key Vault (#5)`

---

### Phase 2: CD Scaffold — Deploy to Staging

**Depends on:** Phase 1.
**Closes:** #4

Steps:

1. Create `.github/workflows/deploy-staging.yml` with `workflow_dispatch` trigger.
2. Workflow steps:
   a. Checkout.
   b. Set up .NET 10.
   c. `dotnet publish` to a staging artifact.
   d. Deploy artifact to the App Service slot via `azure/webapps-deploy` action.
3. Store Azure credentials in GitHub Actions secret `AZURE_CREDENTIALS`.
4. Document the manual trigger steps in `docs/operations/deploy-staging.md`.

**Verification:**

- Workflow YAML is valid (can be linted locally with `actionlint` or the GitHub Actions extension).
- Deployment to staging succeeds end-to-end at least once.

**Commit:** `feat: manual CD workflow for staging deployment (#4)`

---

### Phase 3: SQL Server Support for Cloud Environments

**Closes:** #12
**Depends on:** Phase 1.

Steps:

1. Add `Microsoft.EntityFrameworkCore.SqlServer` package to `ExamSimulator.Web.csproj`.
2. In `Program.cs`, select the EF provider by environment:
   - `Development` → SQLite (connection string `DefaultConnection`).
   - All other environments → SQL Server (connection string `DefaultConnection`, resolved from Key Vault reference at runtime).
3. Add SQL Server connection string placeholder to `appsettings.json` (no real credentials — the real value lives in Key Vault).
4. Verify existing migrations are compatible with SQL Server, or create a new target migration if required.

**Verification:**

- `dotnet build` succeeds.
- `dotnet run` in `Development` still uses SQLite.
- `dotnet ef migrations script` produces valid SQL for SQL Server.

**Commit:** `feat: SQL Server provider for staging/prod environments`

---

### Phase 4: Expand Question Domain Model

**Closes:** #13
**Depends on:** Phase 3 (migration must go on top of Phase 3 database state).

Steps:

1. Add `QuestionType.cs` enum: `SingleChoice`, `MultipleChoice`. (`Ordering` deferred.)
2. Add `Difficulty.cs` enum: `Easy`, `Medium`, `Hard`.
3. Modify `Question.cs`:
   - Add `QuestionType Type` property.
   - Add `Difficulty Difficulty` property.
   - Add `string? Explanation` property (nullable).
   - Replace `int CorrectOptionIndex` with `IReadOnlyList<int> CorrectOptionIndices`.
   - Update constructor signature and invariants:
     - `SingleChoice`: exactly one correct index.
     - `MultipleChoice`: one or more correct indices, all in range, no duplicates.
4. Update `ExamSimulatorDbContext.cs`:
   - Store `Type` and `Difficulty` as int columns (enum backing).
   - Store `CorrectOptionIndices` as JSON column (same pattern as `Options`).
   - Remove the old `CorrectOptionIndex` mapping.
5. Run `dotnet ef migrations add AddTypeAndDifficulty` and verify the generated SQL.
6. Update `ExamSimulator.Web.UnitTests/Questions/QuestionTests.cs`:
   - Adjust existing tests to use `CorrectOptionIndices`.
   - Add tests for `MultipleChoice` invariants (at least one index, no duplicates, indices in range).
   - Add tests covering `Difficulty` and `Explanation`.

**Verification:**

- All domain unit tests pass.
- Migration applies cleanly against a fresh SQLite database.

**Commit:** `feat: add QuestionType, Difficulty, Explanation and multi-correct-option support`

---

### Phase 5: Update CreateQuestion and ListQuestions

**Closes:** #14
**Depends on:** Phase 4.

Steps:

1. Update `CreateQuestion.razor`:
   - Add `QuestionType` radio group (Single Choice / Multiple Choice).
   - When `MultipleChoice` is selected, switch the options list to use checkboxes for correct-answer selection.
   - Add `Difficulty` dropdown (Easy / Medium / Hard).
   - Add `Explanation` textarea (optional).
   - Update `HandleSubmit` to pass `CorrectOptionIndices` list and the new fields to the `Question` constructor.
2. Update `ListQuestions.razor`:
   - Add `Type` and `Difficulty` columns.
   - Add `Edit` and `Delete` action links per row.

**Verification:**

- App renders without errors.
- Can create a single-choice question and a multiple-choice question.
- Both appear correctly in the list.

**Commit:** `feat: update CreateQuestion and ListQuestions for expanded Question model`

---

### Phase 6: EditQuestion Slice

**Closes:** #15
**Depends on:** Phase 5.

Steps:

1. Create `Features/Questions/EditQuestion.razor` at route `/questions/{id:guid}/edit`.
2. On `OnInitializedAsync`, load the question from `DbContext` by Id; redirect to `/questions` with a 404 status if not found.
3. Pre-populate all fields from the loaded question.
4. On submit, update the question by creating a new `Question` domain object with the edited values, then:
   - Remove the old entity.
   - Add the replacement.
   - `SaveChangesAsync`.
   - Redirect to `/questions`.
5. Add a functional test: load edit page for a seeded question, submit changed prompt, verify persisted change.

**Verification:**

- Edit page renders with pre-populated values.
- Saving changes persists correctly.
- Functional test passes.

**Commit:** `feat: EditQuestion slice`

---

### Phase 7: DeleteQuestion Slice

**Closes:** #16
**Depends on:** Phase 5.

Steps:

1. Create `Features/Questions/DeleteQuestion.razor` at route `/questions/{id:guid}/delete`.
2. On `OnInitializedAsync`, load the question; redirect to `/questions` if not found.
3. Display the question prompt and a confirmation button.
4. On confirm, remove the entity and `SaveChangesAsync`, then redirect.
5. Add a functional test: seed a question, confirm delete, verify it is no longer in the database.

**Verification:**

- Delete confirmation page renders.
- Confirming delete removes the record.
- Functional test passes.

**Commit:** `feat: DeleteQuestion slice`

---

## Issues Closed by This Iteration

| Issue | Closed by |
|-------|-----------|
| #5 Infra baseline (IaC skeleton) | Phase 1 commit |
| #4 CD scaffold (manual deploy workflow) | Phase 2 commit |
| #12 SQL Server provider for staging/prod | Phase 3 commit |
| #13 Expand Question domain model | Phase 4 commit |
| #14 Update CreateQuestion and ListQuestions | Phase 5 commit |
| #15 EditQuestion slice | Phase 6 commit |
| #16 DeleteQuestion slice | Phase 7 commit |

## Deferred to Iteration 3

- Ordering question type (`QuestionType.Ordering`).
- ExamProfile entity and management pages.
- CreateExam and published-exam management.
- Branch protection rules on `main`.
- Staging smoke test in CD workflow.
- Markdown rendering for Explanation field.
