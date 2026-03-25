# Iteration 8 Plan: Exam Configuration, Question Filtering & Attempt History

## Current State (End of Iteration 7)

- Bulk JSON import with validation, preview, and idempotent re-import. ✅
- 470 questions can now be imported into a single exam profile. ✅
- Exam session takes all questions from the profile, shuffled — no filtering or sizing. ❌
- Question admin list shows all questions flat, no filtering or pagination. ❌
- Completed exam attempts are never persisted — no score history. ❌

## Goals

1. Add an exam configuration step before starting a session: filter by topic tag and difficulty, choose question count.
2. Persist every completed attempt with per-question correctness for later analysis.
3. Show attempt history per exam profile (date, score, total, tags used).
4. Add server-side filtering to the question admin list (by exam profile, topic tag, difficulty).

## Key Decisions

### Goal 1: Exam Session Configuration

**Where:** `ExamSession.razor` at route `/exams/{profileId}`. A new `Configuring` state is added to the existing `SessionState` enum — no new page, no route change.

**Page-load query:** On `OnInitializedAsync`, instead of loading all questions, execute a single lightweight aggregate query:

```sql
SELECT TopicTag, Difficulty, COUNT(*) AS Count
FROM Questions
WHERE ExamProfileId = @profileId
GROUP BY TopicTag, Difficulty
```

This returns at most ~30 rows (10 tags × 3 difficulties) and is held in component state as a `List<TagDifficultyCount>`.

**Config UI (Configuring state):**
- Exam profile name + description as heading.
- **Topic tags** — one checkbox per distinct tag, all checked by default.
- **Difficulty** — three checkboxes: Easy, Medium, Hard — all checked by default.
- **Question count** — numeric input, defaulting to `min(60, MatchingCount)`.
- **Question order** — radio toggle: `● Random (default)` / `○ Sequential (by topic)`. Sequential uses the existing natural sort (`TopicTag` then `Id`), grouping questions by topic in alphabetical order.
- **Matching count display** — computed property (no server call), updated on every checkbox change:
  ```csharp
  private int MatchingCount => _counts
      .Where(c => _selectedTags.Contains(c.Tag) && _selectedDifficulties.Contains(c.Difficulty))
      .Sum(c => c.Count);
  ```
- **Count auto-clamp** — if `_requestedCount > MatchingCount`, it is silently clamped to `MatchingCount` before the session starts; a notice is shown: *"Only N questions match your filters."*
- **Start button states:**
  - `MatchingCount == 0` → disabled + message: *"No questions match your selection. Adjust the filters."*
  - `MatchingCount >= 1` → enabled. No enforced minimum beyond zero — the user may intentionally take a very short session.

**Minimum count:** 1. No artificial floor — blocking at zero is sufficient.

**Session start (on "Start Exam" click):**
- Server query: `WHERE ExamProfileId = ? AND TopicTag IN (?) AND Difficulty IN (?)`, fetched in full.
- If `_randomOrder`: `.OrderBy(_ => Random.Shared.Next()).Take(_requestedCount)`.
- If sequential: `.OrderBy(q => q.TopicTag).ThenBy(q => q.Id).Take(_requestedCount)`.
- Transitions `_state` to `InProgress`.

**"Reconfigure" link** — shown in the session header during `InProgress`. Clicking it shows a confirmation dialog: *"You have answered X of Y questions. Starting over will discard your progress. Continue?"* On confirm: resets `_answers`, `_currentIndex`, and `_questions`; returns to `Configuring` state with filter values preserved.

---

### Goal 2: Attempt History — Data Model

**New entities:**

```
ExamAttempt
  Id          Guid        PK
  UserId      string      FK → AspNetUsers.Id
  ProfileId   string      FK → ExamProfiles.Id
  TakenAt     DateTime    UTC
  Score       int         correctly answered questions
  Total       int         total questions in this attempt
  Tags        string      JSON array of tag names used (snapshot of filters applied)
  Difficulties string     JSON array of difficulties used
  RandomOrder  bool        whether questions were drawn in random order

ExamAttemptAnswer
  Id          Guid        PK
  AttemptId   Guid        FK → ExamAttempt.Id (cascade delete)
  QuestionId  Guid        FK → Questions.Id (no cascade — question may be deleted later)
  IsCorrect   bool
```

**Why store `Tags` and `Difficulties` on `ExamAttempt`:** the filter snapshot preserves context even if questions are later imported/deleted. Enables future per-topic trend queries without joining back through question data.

**Why `IsCorrect` only on `ExamAttemptAnswer`:** stores the minimum needed for weak-topic analysis. Selected indices are not stored — the goal is progress tracking, not attempt replay.

**EF migration:** one new migration `AddAttemptHistory` creating both tables.

**DI registration:** no new service needed — `ExamSession.razor` writes directly to `DbContext` at submit time, consistent with the existing pattern.

---

### Goal 2: Attempt Persistence — Write Path

In `ExamSession.razor`, at the end of the existing `Submit()` method:

1. Resolve the current user's `UserId` via `AuthenticationStateProvider`.
2. Create an `ExamAttempt` record.
3. Create one `ExamAttemptAnswer` per question in `_questions`, setting `IsCorrect` from the per-question scoring already computed in the `foreach` loop.
4. `DbContext.SaveChangesAsync()`.
5. Store the saved `AttemptId` in component state — used for linking the results page to its history entry in future iterations.

If the save fails (e.g. transient DB error), the session review is still shown — the error is logged but not surfaced to the user as a blocking dialog. Attempt persistence is best-effort.

---

### Goal 3: Attempt History Display

**Where:** `ExamList.razor` at `/exams` — now requires `[Authorize]` (consistent with the access control model established in iteration 6). The existing exam profile card gets a "Recent attempts" section below the Start button showing the last 5 attempts for the current user.

**Query:** join `ExamAttempts` filtered by `UserId` and `ProfileId`, `OrderByDescending(a => a.TakenAt).Take(5)`.

**Display per row:** date (local time), score/total, percentage badge (green ≥ 70%, amber 50–69%, red < 50%).

**Scope:** current user's own attempts only. An admin view of all users' attempts (`/admin/attempts`) is deferred to a later iteration.

**"View all" link** deferred — the last 5 is sufficient for iteration 8.

---

### Goal 4: Question List Filtering

**Where:** `ListQuestions.razor` at `/questions`. Filter controls are added above the existing table.

**Filter controls:**
- Exam profile dropdown (all profiles + "All profiles" option).
- Topic tag dropdown (populated from distinct tags for the selected profile, or all tags if "All profiles").
- Difficulty dropdown: All / Easy / Medium / Hard.

**Behaviour:**
- All filters default to "All" on page load.
- Each filter change triggers a new server query — no client-side filtering.
- The query builds a `IQueryable<Question>` with `.Where()` clauses applied conditionally.
- Tag dropdown is re-populated when the profile selection changes.

**Pagination:** not in scope for iteration 8 — server-side filtering alone reduces the visible set to a manageable size for admin use.

---

### Deferred to Iteration 9

- Predefined filter presets ("Save as preset" from current filter state).
- Admin view of all users' attempts (`/admin/attempts`).
- "View all attempts" history page per user.
- Per-question review of past attempts (replay what you answered).
- Question list pagination.

---

## Target State (End of Iteration 8)

```
src/
  ExamSimulator.Web/
    Domain/
      Attempts/
        ExamAttempt.cs             ← new entity
        ExamAttemptAnswer.cs       ← new entity
    Features/
      Exams/
        ExamSession.razor          ← Configuring state, aggregate load, filter UI,
                                      attempt persistence on Submit
        ExamList.razor             ← recent attempts per profile card
      Questions/
        ListQuestions.razor        ← profile / tag / difficulty filter controls
    Infrastructure/
      ExamSimulatorDbContext.cs    ← DbSet<ExamAttempt>, DbSet<ExamAttemptAnswer>
      Migrations/
        ..._AddAttemptHistory.cs   ← new migration

tests/
  ExamSimulator.Web.UnitTests/
    Attempts/
      ExamAttemptTests.cs          ← domain invariant tests for ExamAttempt
  ExamSimulator.Web.FunctionalTests/
    ExamTests.cs                   ← extended: config page loads, attempt recorded on submit
```

---

## Phases

---

### Phase 1: Domain Model + EF Migration

**Changes:**
- Create `ExamAttempt.cs` and `ExamAttemptAnswer.cs` under `Domain/Attempts/`.
- Add `DbSet<ExamAttempt>` and `DbSet<ExamAttemptAnswer>` to `ExamSimulatorDbContext`.
- Configure relationships: `ExamAttempt` → `ExamAttemptAnswer` (one-to-many, cascade delete). `ExamAttemptAnswer.QuestionId` is a plain column (no FK constraint) to survive question deletion.
- `Tags` and `Difficulties` stored as `nvarchar(max)` with `HasConversion` to `List<string>` via JSON — same pattern as `Options` on `Question`.
- Add EF migration `AddAttemptHistory`.

**Definition of Done:** migration applies cleanly; both tables exist in the dev database.

---

### Phase 2: Exam Session Configuration

**Changes:**
- Add `[Authorize]` to `ExamList.razor`.
- Add `Configuring` to `SessionState` enum; set as initial state after profile + aggregate load.
- Replace `OnInitializedAsync` question load with the aggregate GROUP BY query into `List<TagDifficultyCount>`.
- Add `_selectedTags`, `_selectedDifficulties`, `_requestedCount`, `_randomOrder` (default `true`) fields.
- Add `MatchingCount` computed property.
- Implement config UI markup: tag checkboxes, difficulty checkboxes, count input, order toggle (Random / Sequential), matching count display, Start button.
- On Start: execute filtered question query, clamp count, transition to `InProgress`.
- Add "Reconfigure" link in session header.

**Definition of Done:** user can configure and start a filtered session; zero-match state disables Start; over-requested count is clamped with a notice.

---

### Phase 3: Attempt Persistence

**Changes:**
- Inject `AuthenticationStateProvider` into `ExamSession.razor`.
- At end of `Submit()`: create `ExamAttempt` + `ExamAttemptAnswer` records and `SaveChangesAsync`.
- Store filter snapshot (`Tags`, `Difficulties`, `RandomOrder`) on the attempt.
- Wrap in try/catch; log on failure; do not block review display.

**Definition of Done:** completing an exam creates a row in `ExamAttempts` and one row per question in `ExamAttemptAnswers`.

---

### Phase 4: Attempt History on Exam List

**Changes:**
- In `ExamList.razor`, extend `OnInitializedAsync` to also load the last 5 attempts per profile for the current user.
- Add attempt history rows to each exam profile card: date, score/total, colour-coded percentage badge.

**Definition of Done:** a user who has completed at least one attempt sees their recent scores on the exam list.

---

### Phase 5: Question List Filtering

**Changes:**
- Add profile dropdown, tag dropdown, and difficulty dropdown to `ListQuestions.razor`.
- Implement `ApplyFilters()` method building a conditional `IQueryable<Question>`.
- Re-populate tag dropdown on profile change.

**Definition of Done:** admin can filter the question list by any combination of profile, tag, and difficulty; a filtered result is returned from the server.

---

### Phase 6: Tests

**Changes:**
- `ExamAttemptTests.cs` (unit): attempt requires non-empty userId and profileId; score cannot exceed total; answer list count must match total.
- `ExamTests.cs` (functional): extend with — config page renders for authenticated user; start with all-defaults creates a session; attempt row exists in DB after submit.

**Definition of Done:** all existing tests pass; new tests pass.
