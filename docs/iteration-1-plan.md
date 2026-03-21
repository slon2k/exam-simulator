# Iteration 1 Plan: Restructure to VSA + First Feature Slice

## Goal

Collapse the 3-project Clean Architecture layout into a single `ExamSimulator.Web` project organized by feature folders (Vertical Slice Architecture), then build the first complete slice (CreateQuestion) end-to-end: Blazor Server UI + EF Core persistence + domain logic, all owned by a single feature folder.

## Key Decisions

- **Blazor Server** stays (per plan section 3).
- **Single project VSA**: Domain and Infrastructure become folders inside Web, not separate projects.
- **EF maps domain `Question` directly** — no separate persistence entity for Stage 1.
- **SQLite for dev**, SQL Server deferred until cloud deploy (Iteration 2).
- **Options stored as JSON column** in EF (avoids a separate QuestionOption table for now).
- **No Application project** — slice handlers live in feature folders.
- Test project renamed `ExamSimulator.Domain.UnitTests` → `ExamSimulator.Web.UnitTests` to match application structure.

## Target Folder Structure

```
src/
  ExamSimulator.Web/
    Features/
      Questions/
        CreateQuestion.razor
        ListQuestions.razor
    Domain/
      Questions/
        Question.cs
    Infrastructure/
      ExamSimulatorDbContext.cs
      Migrations/
    Components/
      Layout/
      Pages/
    Program.cs
tests/
  ExamSimulator.Web.UnitTests/
    Questions/
      QuestionTests.cs
  ExamSimulator.Web.FunctionalTests/
```

## Phases

### Phase 1: Restructure to VSA

**Depends on:** Issue #6 merged (done).

Steps:

1. Switch to `main`, pull.
2. Remove `ExamSimulator.Infrastructure` project from solution (only has `Class1.cs` placeholder).
3. Move `ExamSimulator.Domain/Questions/Question.cs` into `ExamSimulator.Web/Domain/Questions/Question.cs`.
4. Remove `ExamSimulator.Domain` project from solution.
5. Delete empty `src/ExamSimulator.Domain/` and `src/ExamSimulator.Infrastructure/` directories.
6. Update `ExamSimulator.Web.csproj` — remove ProjectReferences to Domain and Infrastructure.
7. Rename test project: `ExamSimulator.Domain.UnitTests` → `ExamSimulator.Web.UnitTests` (folder, .csproj filename, and namespace).
8. Update `ExamSimulator.Web.UnitTests.csproj` — change ProjectReference to point at `ExamSimulator.Web`.
9. Fix namespaces: `ExamSimulator.Domain.Questions` → `ExamSimulator.Web.Domain.Questions` (in Question.cs), `ExamSimulator.Domain.UnitTests` → `ExamSimulator.Web.UnitTests` (in QuestionTests.cs).
10. Create the VSA folder structure inside Web: `Features/Questions/`, `Domain/Questions/`, `Infrastructure/`.
11. Update `ExamSimulator.slnx` — remove Domain and Infrastructure entries, update unit test project path.
12. Delete placeholder `UnitTest1.cs` from `ExamSimulator.Web.FunctionalTests`.

**Verification:**

- `dotnet build` succeeds.
- `dotnet test` — all 10 domain unit tests pass.

**Commit:** `refactor: collapse to single-project VSA structure`

### Phase 2: EF Core + SQLite Dev Setup

**Depends on:** Phase 1.
**Closes:** #9

Steps:

1. Add NuGet packages to `ExamSimulator.Web.csproj`:
   - `Microsoft.EntityFrameworkCore.Sqlite`
   - `Microsoft.EntityFrameworkCore.Design`
2. Create `Infrastructure/ExamSimulatorDbContext.cs` with `DbSet<Question> Questions`.
3. Configure EF mapping for `Question` (table name `Questions`, Options stored as JSON column).
4. Register DbContext in `Program.cs` with SQLite connection string for Development.
5. Run `dotnet ef migrations add InitialCreate`.
6. Run `dotnet ef database update` to verify.

**Verification:**

- Migration generates successfully.
- Database file created and schema correct.

**Commit:** `feat: EF Core setup with SQLite and initial migration`

### Phase 3: CreateQuestion + ListQuestions Slices

**Depends on:** Phase 2.

Steps:

1. Create `Features/Questions/CreateQuestion.razor` — Blazor Server form:
   - ExamProfileId, Prompt, TopicTag fields.
   - Dynamic options list (add/remove, min 2).
   - CorrectOptionIndex selector.
   - Submit button.
2. Create `Features/Questions/ListQuestions.razor` — simple table of existing questions.
3. Wire navigation in `Components/Layout/NavMenu.razor`.
4. Form submit → create `Question` domain object (invariants enforced) → save via DbContext.

**Verification:**

- App starts, CreateQuestion page renders.
- Can create a question and see it in the list.

**Commit:** `feat: CreateQuestion and ListQuestions Blazor pages (closes #2)`

### Phase 4: Functional Test

**Depends on:** Phase 3.

Steps:

1. Update `ExamSimulator.Web.FunctionalTests` to use `WebApplicationFactory` with SQLite in-memory.
2. Add one happy-path test: render CreateQuestion page, submit valid form, verify question persisted.
3. Run all tests — domain unit tests + functional test.

**Verification:**

- All tests pass (unit + functional).

**Commit:** `test: functional test for CreateQuestion slice`

## Issues Closed by This Iteration

| Issue | Closed by |
|-------|-----------|
| #6 Question domain entity | Merged |
| #3 CI baseline | Already done (CI workflow pushed) |
| #9 EF Core + SQLite dev setup | Phase 2 commit |
| #2 Admin create/list question | Phase 3 commit |

## Deferred to Iteration 2

- #4 CD scaffold (manual deploy workflow)
- #5 Infra baseline (IaC skeleton)
- SQL Server configuration (replace SQLite)
- Branch protection rules on main
