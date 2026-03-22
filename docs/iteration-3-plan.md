# Iteration 3 Plan: ExamProfile Entity + Exam-Taking Flow

## Current State (End of Iteration 2)

- VSA single-project structure in place. ✅
- `Question` domain entity with Type, Difficulty, Explanation, CorrectOptionIndices. ✅
- Full admin CRUD for Questions (Create, List, Edit, Delete). ✅
- EF Core + SQL Server, migrations running on startup. ✅
- Bicep IaC for App Service, SQL, Key Vault. ✅
- Manual CD workflow for staging. ✅
- 28 domain unit tests + 4 functional tests, all passing. ✅
- `ExamProfileId` is a plain string — no ExamProfile entity or admin pages. ❌
- No learner-facing exam-taking UI. ❌

## Goals

1. Introduce `ExamProfile` as a proper domain entity with its own table and admin CRUD pages.
2. Turn `Question.ExamProfileId` into a real FK relationship.
3. Build the core learner exam-taking flow: pick a profile → answer questions one at a time → submit → see score.

## Key Decisions

- **ExamProfile.Id = string slug** (e.g. "az-204") — natural primary key, matches the existing string values already stored in `Question.ExamProfileId` (no data migration required beyond seeding profile rows).
- **FK constraint at EF level** with `DeleteBehavior.Restrict` — prevent deleting a profile that still has questions.
- **Exam sessions are in-memory only** — no `ExamAttempt` persistence (deferred to Iteration 4).
- **No timing, no randomisation, no explanation overlay** — deferred.
- **`Ordering` question type** — still deferred.
- **Markdown rendering for Explanation** — still deferred.

## Target State (End of Iteration 3)

```
src/
  ExamSimulator.Web/
    Domain/
      ExamProfiles/
        ExamProfile.cs           ← new entity
    Features/
      ExamProfiles/
        ListExamProfiles.razor   ← new admin page
        CreateExamProfile.razor  ← new admin page
        EditExamProfile.razor    ← new admin page
      Exams/
        ExamList.razor           ← new learner page
        ExamSession.razor        ← new learner page
      Questions/
        CreateQuestion.razor     ← ExamProfileId input → dropdown
        EditQuestion.razor       ← ExamProfileId input → dropdown
    Infrastructure/
      ExamSimulatorDbContext.cs  ← new DbSet, FK config
      Migrations/
        ..._AddExamProfile.cs    ← new migration
tests/
  ExamSimulator.Web.UnitTests/
    ExamProfiles/
      ExamProfileTests.cs        ← new
  ExamSimulator.Web.FunctionalTests/
    ExamTests.cs                 ← new
```

## Phases

---

### Phase 1: ExamProfile Domain + Persistence

**Closes:** #20

Steps:

1. Create `src/ExamSimulator.Web/Domain/ExamProfiles/ExamProfile.cs`:
   - `string Id` — natural PK (slug, e.g. "az-204"); validated: non-empty, trimmed, lowercase letters/digits/hyphens only.
   - `string Name` — display name (e.g. "Azure Developer Associate AZ-204"); validated: non-empty, trimmed.
   - `string? Description` — optional free text.
   - Constructor enforces all invariants.

2. Update `ExamSimulatorDbContext.cs`:
   - Add `DbSet<ExamProfile> ExamProfiles`.
   - Configure `ExamProfiles` table with string PK.
   - Add FK: `HasOne<ExamProfile>().WithMany().HasForeignKey(q => q.ExamProfileId).OnDelete(DeleteBehavior.Restrict)` on the Questions configuration.

3. Run `dotnet ef migrations add AddExamProfile` and verify the generated SQL.

4. Update `DbSeeder.cs`:
   - Seed `ExamProfile` rows **before** seeding Questions; seed only if the ExamProfiles table is empty.
   - At minimum, seed one entry matching the existing seeded questions' `ExamProfileId` value ("az-204").

**Verification:**

- `dotnet build` succeeds.
- App starts, migration applies, seed data includes ExamProfile rows.
- FK enforced: attempting to delete a profile with questions is blocked.

**Commit:** `feat: ExamProfile entity, FK to Questions, AddExamProfile migration`

---

### Phase 2: ExamProfile Admin CRUD

**Depends on:** Phase 1.
**Closes:** #21

Steps:

1. Create `Features/ExamProfiles/ListExamProfiles.razor` at `@page "/exam-profiles"`:
   - Load all profiles with question count on init (`AsNoTracking()`).
   - Table columns: Id, Name, Description, Question Count, Actions (Edit, Delete).
   - Delete: JS confirm dialog → fetch tracked entity by Id → `Remove` → `SaveChangesAsync`.

2. Create `Features/ExamProfiles/CreateExamProfile.razor` at `@page "/exam-profiles/create"`:
   - Form fields: Id (slug), Name, optional Description.
   - On submit: construct `ExamProfile`, add to DbContext, save, navigate to `/exam-profiles`.
   - Show validation errors inline.

3. Create `Features/ExamProfiles/EditExamProfile.razor` at `@page "/exam-profiles/{Id}"`:
   - `[Parameter] string Id`
   - Load by Id on init; show 404 message if not found.
   - Same form fields as Create; on submit: update properties, save, navigate to `/exam-profiles`.

4. Update `Features/Questions/CreateQuestion.razor`:
   - Replace the free-text `ExamProfileId` `<input>` with a `<select>` dropdown.
   - Load `ExamProfiles` on init (`AsNoTracking().OrderBy(p => p.Name)`).

5. Update `Features/Questions/EditQuestion.razor`:
   - Same dropdown change as CreateQuestion.

**Verification:**

- Can create, view, edit, delete an ExamProfile.
- CreateQuestion and EditQuestion show a dropdown of available profiles.
- Deleting a profile with attached questions shows an error (not a crash).

**Commit:** `feat: ExamProfile admin CRUD pages, Questions dropdown`

---

### Phase 3: Exam-Taking Flow

**Depends on:** Phase 1.
**Closes:** #22

Steps:

1. Create `Features/Exams/ExamList.razor` at `@page "/exams"`:
   - Load all ExamProfiles with question count on init.
   - Render as a table: Name, Description, question count, "Start Exam" button.
   - "Start Exam" navigates to `/exams/{profile.Id}`.
   - Show "No exams available" if no profiles exist.

2. Create `Features/Exams/ExamSession.razor` at `@page "/exams/{ProfileId}"`:
   - `[Parameter] string ProfileId`
   - On init: load all questions for the profile (`AsNoTracking().Where(q => q.ExamProfileId == ProfileId).ToListAsync()`). Show message if none found.
   - Component state:
     - `int _currentIndex = 0`
     - `Dictionary<Guid, List<int>> _answers = new()`
     - `bool _submitted = false`
   - Progress indicator: "Question X of Y".
   - Render current question:
     - `SingleChoice`: radio buttons for each option.
     - `MultipleChoice`: checkboxes for each option.
   - "Next" button when not on last question; "Submit" on last.
   - After submit: compute results per question (compare `_answers[q.Id]` vs `q.CorrectOptionIndices`), show score panel: "You scored X / Y" + per-question pass/fail.
   - "Retake" button resets state to index 0.

**Verification:**

- `/exams` lists profiles with question counts.
- Can answer all questions and submit to see a score.
- Single-choice and multiple-choice questions render with the correct input type.
- Profile with no questions shows an appropriate message.

**Commit:** `feat: exam-taking flow — ExamList and ExamSession pages`

---

### Phase 4: Navigation Update

**Depends on:** Phases 2 and 3.
**Closes:** #23

Steps:

1. Update `Components/Layout/NavMenu.razor`:
   - Add "Exam Profiles" link → `/exam-profiles`.
   - Add "Take an Exam" link → `/exams`.

**Commit:** `feat: add Exam Profiles and Take an Exam nav links`

---

### Phase 5: Tests

**Depends on:** Phases 1–3.
**Closes:** #24

Steps:

1. Create `tests/ExamSimulator.Web.UnitTests/ExamProfiles/ExamProfileTests.cs`:
   - Valid construction with Id, Name, optional Description.
   - Rejects blank/null Id; rejects blank/null Name.
   - Rejects invalid slug format (uppercase, spaces, special characters).
   - Uses xUnit Assert (no FluentAssertions).

2. Create `tests/ExamSimulator.Web.FunctionalTests/ExamTests.cs`:
   - `GET /exam-profiles` returns 200.
   - `GET /exams` returns 200.

**Verification:**

- All existing 28 unit tests + 4 functional tests still pass.
- All new ExamProfile unit tests pass.
- New functional tests pass.

**Commit:** `test: ExamProfile unit tests and exam flow functional tests`

---

### Phase 6: Deploy to Staging

**Depends on:** All phases.

Steps:

1. Commit and push all changes.
2. Trigger the manual staging deploy workflow.
3. Smoke test: `/exam-profiles`, `/exams`, `/questions/create` (dropdown) all load correctly in staging.
