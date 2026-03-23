# Iteration 5 Plan: BuildList + Matching Question Types

## Current State (End of Iteration 4)

- `QuestionType`: `SingleChoice = 0`, `MultipleChoice = 1`, `Ordering = 2`. ✅
- `Question.Options` — full option pool (JSON column). ✅
- `Question.CorrectOptionIndices` — correct indices (JSON column). ✅
- No `MatchingTargets` field on `Question`. ❌
- `BuildList` question type — deferred. ❌
- `Matching` question type — deferred. ❌
- 32 tests (29 unit + 7 functional), all passing. ✅
- Staging deploy complete. ✅

## Goals

1. Implement the `BuildList` question type: learner selects an ordered subset from a larger pool.
2. Implement the `Matching` question type: learner pairs each premise with its correct response.

## Key Decisions

### BuildList

- **Type value**: `QuestionType.BuildList = 3`.
- **Domain model**: `Options` = full pool (M items). `CorrectOptionIndices` = ordered subset of K items where `2 <= K < M`. Invariant: `Count >= 2 && Count < Options.Count`, all distinct, all in range.
- **No new DB column** — reuses existing `Options` + `CorrectOptionIndices` schema. No EF migration required.
- **Admin UX**: two-panel. Top panel = pool (all option text inputs, each with an "Add →" button). Bottom panel = answer list (indices in correct order, with Up ↑ / Down ↓ / Remove ✕ buttons). Pool items already in the answer list are greyed out.
- **Session UX**: same two-panel split. Left = available items (click to add). Right = selected answer (Up/Down/Remove). Stored as `_answers[q.Id] = List<int>` (initially empty).
- **Scoring**: `SequenceEqual` — selected list must match `CorrectOptionIndices` exactly (both identity and order).

### Matching

- **Type value**: `QuestionType.Matching = 4`.
- **Domain model**: `Options` = premises (left column). New `IReadOnlyList<string>? MatchingTargets` = responses (right column, may include distractors). `CorrectOptionIndices[i]` = index in `MatchingTargets` that `Options[i]` maps to.
- **Invariant**: `MatchingTargets.Count >= Options.Count` (at least one response per premise, distractors allowed), `CorrectOptionIndices.Count == Options.Count`, all indices in `[0, MatchingTargets.Count)`, all distinct (each target is the correct match for at most one premise).
- **New DB column**: `MatchingTargets` nullable JSON text column — same EF `HasConversion` + `ValueComparer` pattern as `Options`. New EF migration: `AddMatchingTargets`.
- **Admin UX**: three sections. (1) Premises — text inputs with Add/Remove. (2) Responses — text inputs with Add/Remove (distractors can be added here). (3) Pairs — for each premise, a dropdown selecting its correct response from the responses list.
- **Session UX**: for each premise, a dropdown populated with all `MatchingTargets` (shuffled). Stored as `_answers[q.Id] = Dictionary<int, int?>` (premise index → selected target index, null = not yet answered).
- **Scoring**: `CorrectOptionIndices[i] == userAnswer[i]` for all premises.

### General

- **Seeder**: add one `BuildList` sample question and one `Matching` sample question to `DbSeeder.cs`.
- **Attempt persistence** — still deferred to Iteration 7 (after Iteration 6: Authentication).

## Target State (End of Iteration 5)

```
src/
  ExamSimulator.Web/
    Domain/
      Questions/
        QuestionType.cs          ← adds BuildList = 3, Matching = 4
        Question.cs              ← new MatchingTargets property, BuildList + Matching invariants
    Features/
      Questions/
        CreateQuestion.razor     ← BuildList two-panel, Matching premises/responses/pairs UI
        EditQuestion.razor       ← same
      Exams/
        ExamSession.razor        ← BuildList two-panel answer UI, Matching dropdown answer UI, scoring
    Infrastructure/
      ExamSimulatorDbContext.cs  ← MatchingTargets column config
      Migrations/
        ..._AddMatchingTargets.cs ← new migration
      DbSeeder.cs                ← new BuildList + Matching seed questions
tests/
  ExamSimulator.Web.UnitTests/
    Questions/
      QuestionTests.cs           ← BuildList + Matching invariant tests
  ExamSimulator.Web.FunctionalTests/
    QuestionAdminTests.cs        ← BuildList + Matching round-trip tests
```

## Phases

---

### Phase 1: BuildList Question Type

Steps:

1. Add `BuildList = 3` to `Domain/Questions/QuestionType.cs`.

2. Add `BuildList` invariant in `Question` constructor (after the existing `Ordering` check):
   ```csharp
   if (type == QuestionType.BuildList)
   {
       if (indexList.Count < 2)
           throw new ArgumentException("BuildList questions must have at least 2 items in the answer.", nameof(correctOptionIndices));
       if (indexList.Count >= optionList.Count)
           throw new ArgumentException("BuildList answer must be a proper subset of the options pool.", nameof(correctOptionIndices));
   }
   ```
   The existing shared validation (all distinct, all in range) already covers the rest.

3. Update `Features/Questions/CreateQuestion.razor`:
   - When type = `BuildList`: render a two-panel UI below the options list.
     - Pool panel (list of all option entries): each row has an "Add →" button disabled if the option index is already in the answer list.
     - Answer panel (ordered answer): each row shows the option text + Up / Down / Remove buttons.
   - `correctOptionIndices` on save = the ordered answer panel index list.
   - Options text entry remains the same as Ordering (plain text inputs).

4. Update `Features/Questions/EditQuestion.razor` with the same changes. On load, reconstruct the answer panel from the saved `CorrectOptionIndices`.

5. Update `Features/Exams/ExamSession.razor`:
   - For `BuildList` questions: render two panels (Available items / Your answer).
   - `_answers[q.Id]` initialised as an empty `List<int>`.
   - Clicking an available item appends its index to the answer list; Up/Down/Remove buttons work the same as the Ordering type.
   - Scoring: `q.CorrectOptionIndices.SequenceEqual(userAnswer)`.

**Verification:**

- Create a BuildList question with a pool of 5 items and an answer of 3. Save. Edit. Verify answer panel re-populates.
- In exam session: build the correct answer → submit → marked correct. Build the wrong subset or wrong order → marked incorrect.
- `dotnet build` succeeds with 0 errors.

**Commit:** `feat: BuildList question type — domain invariant, admin UI, session render + scoring`

---

### Phase 2: Matching Question Type

Steps:

1. Add `Matching = 4` to `Domain/Questions/QuestionType.cs`.

2. Add `IReadOnlyList<string>? MatchingTargets { get; private set; }` property to `Question`.

3. Add `IEnumerable<string>? matchingTargets = null` parameter to the `Question` constructor.

4. Add `Matching` invariant in the constructor:
   ```csharp
   if (type == QuestionType.Matching)
   {
       if (matchingTargets is null)
           throw new ArgumentNullException(nameof(matchingTargets), "Matching questions require matching targets.");
       var targetList = matchingTargets.ToList();
       if (targetList.Count < optionList.Count)
           throw new ArgumentException("Matching targets must have at least as many entries as premises.", nameof(matchingTargets));
       if (targetList.Any(string.IsNullOrWhiteSpace))
           throw new ArgumentException("Each matching target must contain text.", nameof(matchingTargets));
       if (indexList.Count != optionList.Count)
           throw new ArgumentException("Matching questions must have one correct pairing per premise.", nameof(correctOptionIndices));
       if (indexList.Any(i => i < 0 || i >= targetList.Count))
           throw new ArgumentOutOfRangeException(nameof(correctOptionIndices), "All pairing indices must be within the matching targets range.");
       MatchingTargets = targetList.Select(static t => t.Trim()).ToArray();
   }
   ```
   For non-Matching types, ignore `matchingTargets` (leave `MatchingTargets = null`).
   Also relax the shared "at least one correct index" guard — it already passes since `Count == Options.Count >= 2`.

5. Update `ExamSimulatorDbContext.cs` — add `MatchingTargets` column config inside the `Questions` entity block:
   ```csharp
   entity.Property(q => q.MatchingTargets)
       .IsRequired(false)
       .HasConversion(
           targets => targets == null ? null : JsonSerializer.Serialize(targets, (JsonSerializerOptions?)null),
           json => json == null ? null : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!)
       .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>?>(
           (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
           t => t == null ? 0 : t.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
           t => t == null ? null : (IReadOnlyList<string>)t.ToList()));
   ```

6. Add EF migration:
   ```
   dotnet ef migrations add AddMatchingTargets --project src/ExamSimulator.Web
   ```
   Verify the generated migration adds a nullable `TEXT` column `MatchingTargets` to `Questions`.

7. Update `Features/Questions/CreateQuestion.razor`:
   - When type = `Matching`: show three sections.
     - **Premises** section (replaces the regular options section): text inputs with Add/Remove.
     - **Responses** section: separate text inputs for targets (including any distractors).
     - **Pairs** section: for each premise, a `<select>` dropdown letting the admin pick which response is the correct match.
   - On save: `options` = premises list, `matchingTargets` = responses list, `correctOptionIndices` = the dropdown selections (one int per premise).

8. Update `Features/Questions/EditQuestion.razor` with the same Matching UI. On load, populate responses from `q.MatchingTargets` and reconstruct the dropdowns from `q.CorrectOptionIndices`.

9. Update `Features/Exams/ExamSession.razor`:
   - For `Matching` questions: for each premise, render the premise text + a `<select>` dropdown showing all `MatchingTargets` (in shuffled order). Prepend a blank "— select —" option.
   - `_answers[q.Id]` stored as `List<int?>` (one nullable int per premise, null = not selected).
   - Prevent submission until all premises have a selection (same UX pattern as other types).
   - Scoring: `q.CorrectOptionIndices[i] == _answers[q.Id][i]` for all i.
   - In the results review card: show each premise alongside the learner's chosen target and the correct target.

**Verification:**

- Create a Matching question with 3 premises, 4 responses (1 distractor). Save. Edit. Verify pairs re-populate.
- In exam session: select all correct pairings → submit → marked correct. Change one pairing → marked incorrect.
- `dotnet build` succeeds with 0 errors.

**Commit:** `feat: Matching question type — domain model, MatchingTargets migration, admin UI, session render + scoring`

---

### Phase 3: Seeder Updates

Steps:

1. Add a `BuildList` seed question to `DbSeeder.cs`. Example:
   - Prompt: steps to configure a CI/CD pipeline for an Azure App Service (pool of 6 steps, correct ordered answer of 4).
   - Pool includes 2 distractors (steps that are not part of the correct workflow).
   - Explanation: brief Markdown explanation of the correct flow.

2. Add a `Matching` seed question to `DbSeeder.cs`. Example:
   - Prompt: match each Azure storage service to its primary use case.
   - 3 premises (services), 4 responses (use cases, 1 distractor).
   - Explanation: brief Markdown summary.

**Commit:** `feat: add BuildList and Matching seed questions`

---

### Phase 4: Tests

Steps:

1. Add `BuildList` unit tests to `tests/ExamSimulator.Web.UnitTests/Questions/QuestionTests.cs`:
   - Valid: pool of 5, answer of 3 accepted.
   - Rejects: `CorrectOptionIndices.Count >= Options.Count` (answer is not a proper subset).
   - Rejects: `CorrectOptionIndices.Count < 2`.
   - Rejects: out-of-range index.
   - Rejects: duplicate index.

2. Add `Matching` unit tests:
   - Valid: 3 premises, 4 targets (1 distractor), 3 correct pairings.
   - Rejects: `MatchingTargets.Count < Options.Count` (fewer responses than premises).
   - Rejects: `CorrectOptionIndices.Count != Options.Count`.
   - Rejects: out-of-range pairing index.
   - Rejects: duplicate pairing index (same target matched to two premises).
   - Rejects: null `matchingTargets`.

3. Add functional tests to `tests/ExamSimulator.Web.FunctionalTests/QuestionAdminTests.cs`:
   - `CreateBuildListQuestion_WhenSaved_SubsetPersistedCorrectly` — verifies `CorrectOptionIndices` is a proper subset of the options pool.
   - `CreateMatchingQuestion_WhenSaved_MatchingTargetsAndPairsPersistedCorrectly` — verifies `MatchingTargets` and `CorrectOptionIndices` round-trip correctly through EF.

**Verification:**

- `dotnet test` — all existing 32 tests pass + new tests pass.

**Commit:** `test: BuildList and Matching question unit and functional tests`
