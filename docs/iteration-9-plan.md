# Iteration 9 Plan: Attempt Review, History & Pagination

## Current State (End of Iteration 8)

- Completed attempts are persisted with `IsCorrect` per question, but selected answers are not stored. ❌
- The `Submitted` state already shows a full per-question breakdown inline (correct/incorrect per option, ordering, matching, explanations). ✅
- The review markup is inlined directly in `ExamSession.razor` — no shared component, so it cannot be reused by a history review page. ❌
- No page to review a past attempt. ❌
- No history page — only a compact 5-row table per profile card on the exam list. ❌
- Question admin list loads all 470+ questions flat — no pagination. ❌

## Goals

1. Extend `ExamAttemptAnswer` to record which options the user actually selected.
2. Extract the existing per-question review markup from `ExamSession.razor` into a shared `AttemptReview` component so it can be reused by the history review page.
3. Add "Retake" and "View history →" actions to the `Submitted` state.
4. Add a `/attempts/{attemptId}` page for cold review of any historical attempt (loads from DB).
5. Add a `/history` page listing all attempts for the current user, with an optional `?profileId=` filter.
6. Add server-side pagination to the question admin list (25 per page).
7. Cover new behaviour with unit and functional tests.

## Key Decisions

---

### Goal 1: Schema Extension — `SelectedOptionIndices`

**New column on `ExamAttemptAnswer`:**

```
SelectedOptionIndices  string  nullable — JSON array of int (nvarchar(max))
```

Stored as `IReadOnlyList<int>?` in the domain model using the same `HasConversion` JSON pattern already used for `ExamAttempt.Tags` and `Difficulties`.

**NULL compatibility:** existing rows written in iteration 8 have no selected indices. The column is nullable; the review UI handles `null` gracefully by showing *"Selection not recorded"* for those older rows.

**Semantic per question type:**

| Type | Stored value |
|---|---|
| SingleChoice / MultipleChoice | Indices of selected options, e.g. `[1]` or `[0,2]` |
| Ordering | Permutation of option indices in the user's chosen order |
| BuildList | Option indices in the order the user added them |
| Matching | Canonical `MatchingTargets` indices, one per premise position (`-1` = not set) |

**Matching normalisation:** `_answers[q.Id]` holds indices into the **shuffled** targets list shown during the session. Before persisting, each index is resolved to its position in the canonical `q.MatchingTargets` list so the stored value is stable across sessions:

```csharp
var shuffled = _matchingShuffled[q.Id];
var canonical = selectedShuffledIndices
    .Select(i => i >= 0 ? q.MatchingTargets!.ToList().IndexOf(shuffled[i]) : -1)
    .ToList();
```

**EF migration:** `AddSelectedOptionIndicesToAttemptAnswer` — single `AddColumn`, nullable, no data loss.

**Write path:** in `ExamSession.Submit()`, each `ExamAttemptAnswer` row is extended with the resolved `selectedOptionIndices` from `_answers[q.Id]`.

---

### Goal 2: Shared `AttemptReview` Component

The `Submitted` state already contains a complete per-question review — score header, per-option colour coding, ordering/build-list/matching layouts, and explanations. This markup is extracted into a reusable component so the `/attempts/{id}` page can use it without duplicating code.

**Location:** `Components/AttemptReview.razor`

**Parameters:**

```csharp
[Parameter] public required List<ReviewRow> Rows { get; set; }
```

Where `ReviewRow` is a record:

```csharp
record ReviewRow(
    int Number,
    Question? Question,       // null if question was deleted after the attempt
    bool IsCorrect,
    IReadOnlyList<int>? SelectedIndices);
```

**Rendering:** the existing review markup from `ExamSession.razor` is moved here verbatim, with two additions:
- For questions where `SelectedIndices` is `null` (rows written before Phase 1): show a muted notice *"Answer details not available for this attempt"* but still render the correct answer and explanation.
- For deleted questions (`Question` is null): show a muted placeholder *"This question has been removed."* with the `IsCorrect` badge still visible.

**No navigation:** the component is purely presentational. Both the `Submitted` state (in-memory) and the `/attempts/{id}` page (from DB) construct the `List<ReviewRow>` and pass it in.

---

### Goal 3: Updated `Submitted` State

The existing review markup is replaced with `<AttemptReview Rows="_reviewRows" />`. Two action buttons are added below the score header.

**Layout:**

```
✅  Score: 42 / 60 (70%)

[Retake]    [View history →]

─── AttemptReview component (always visible, same behaviour as today) ───
```

**"Retake":** resets `_answers`, `_currentIndex`, `_questions` and transitions back to `Configuring` with all filter values preserved.

**"View history →":** `href="/history?profileId=@ProfileId"`.

**`ReviewRow` construction (in-memory):** built from `_questions` and `_answers` at the time `Submit()` completes — no DB round-trip needed:

```csharp
private List<ReviewRow> BuildReviewRows() =>
    _questions.Select((q, i) => new ReviewRow(
        Number: i + 1,
        Question: q,
        IsCorrect: _scoredResults[q.Id],
        SelectedIndices: _answers.TryGetValue(q.Id, out var sel) ? sel : null
    )).ToList();
```

`_scoredResults` is the existing per-question correctness dictionary already computed in `Submit()`.

---

### Goal 4: Attempt Review Page `/attempts/{attemptId}`

New Blazor page at `Features/Attempts/AttemptReview.razor`, route `/attempts/{AttemptId:guid}`, `[Authorize]`, `InteractiveServer`.

**Data load:**

```csharp
var attempt = await DbContext.ExamAttempts
    .Include(a => a.Answers)
    .FirstOrDefaultAsync(a => a.Id == AttemptId && a.UserId == currentUserId);
```

If `attempt` is `null` (not found or wrong user), render a *"Attempt not found"* message — not a redirect, to avoid leaking attempt existence to other users via timing.

Load questions:

```csharp
var questionIds = attempt.Answers.Select(a => a.QuestionId).ToList();
var questions = await DbContext.Questions
    .Where(q => questionIds.Contains(q.Id))
    .ToDictionaryAsync(q => q.Id);
```

Build `List<ReviewRow>` from the joined data. Answers whose `QuestionId` is absent from the dictionary (deleted question) get `Question = null`.

**Layout:**

```
← Back to history

AZ-204 — 25 Mar 2026 14:30
Score: 42 / 60 (70%)  [badge]
Tags: app-service, functions   Difficulty: Easy, Medium   Random order

[AttemptReview component — expanded by default on this page]
```

On this dedicated page the review is **expanded by default** (not toggled), since the user navigated here specifically to review.

---

### Goal 5: History Page `/history`

**Route:** `@page "/history"`, `[Authorize]`, `InteractiveServer`.

**Query parameter:** `[SupplyParameterFromQuery] public string? ProfileId { get; set; }`

If `ProfileId` is set, the page filters to that profile. The profile dropdown (see below) defaults to the matching option.

**Data load:**

```csharp
var query = DbContext.ExamAttempts
    .Where(a => a.UserId == currentUserId);

if (!string.IsNullOrEmpty(ProfileId))
    query = query.Where(a => a.ProfileId == ProfileId);

var attempts = await query
    .OrderByDescending(a => a.TakenAt)
    .ToListAsync();
```

All profiles are loaded separately into a `Dictionary<string, string>` (`profileId → name`) for display.

**Layout:**

```
My Exam History                              [All profiles ▼]

Profile       Date                Score   Tags            
AZ-204        25 Mar 2026 14:30   42/60   app-service…    [Review]
AZ-204        24 Mar 2026 10:00   38/60   functions…      [Review]
AZ-900        23 Mar 2026 09:00   80/80   –               [Review]
```

The profile dropdown changes the URL (`NavigationManager.NavigateTo("/history?profileId=...")`) — no JS, standard Blazor navigation.

Percentage badge uses the same colour coding as the exam list cards (green ≥ 70%, amber 50–69%, red < 50%).

**"Review" link:** `href="/attempts/{attempt.Id}"`.

**Exam list change:** the "Recent attempts" table on each profile card gets a `View all →` link: `href="/history?profileId={profile.Id}"`.

---

### Goal 6: Question List Pagination

**State added to `ListQuestions.razor`:**

```csharp
private int _page = 1;
private const int PageSize = 25;
private int _totalCount;
```

**`ApplyFilters()` change:**

```csharp
_totalCount = await query.CountAsync();
_questions = await query
    .OrderBy(q => q.TopicTag).ThenBy(q => q.Id)
    .Skip((_page - 1) * PageSize)
    .Take(PageSize)
    .ToListAsync();
```

Any filter change resets `_page = 1` before calling `ApplyFilters()`.

**Pagination bar** below the table:

```
Showing 26–50 of 470      [← Previous]  Page 2 of 19  [Next →]
```

`[← Previous]` and `[Next →]` call `ChangePage(int delta)`:

```csharp
private async Task ChangePage(int delta)
{
    _page = Math.Clamp(_page + delta, 1, TotalPages);
    await ApplyFilters();
}

private int TotalPages => _totalCount == 0 ? 1 : (int)Math.Ceiling(_totalCount / (double)PageSize);
```

Buttons are disabled at the boundary pages.

---

### Goal 7: Tests

**Unit tests — `Attempts/ExamAttemptAnswerTests.cs`:**
- `SelectedOptionIndices` defaults to null when not provided
- Stores single-choice selection correctly
- Stores multi-choice selection correctly
- Stores null without throwing

**Functional tests — `AttemptReviewTests.cs`:**
- Authenticated GET to `/attempts/{id}` for own attempt returns 200
- GET to `/attempts/{id}` belonging to a different user returns 200 with "not found" message (not a 403, to avoid leaking)
- Unauthenticated GET to `/attempts/{id}` redirects to login

**Functional tests — `HistoryTests.cs`:**
- `/history` returns 200 for authenticated user
- `/history?profileId=az-204` returns 200 and filters correctly
- Unauthenticated GET to `/history` redirects to login

---

## Target State (End of Iteration 9)

```
src/
  ExamSimulator.Web/
    Components/
      AttemptReview.razor              ← new shared review component
    Domain/
      Attempts/
        ExamAttemptAnswer.cs           ← extended: SelectedOptionIndices (nullable)
    Features/
      Attempts/
        AttemptReview.razor            ← new page: /attempts/{attemptId}
        History.razor                  ← new page: /history[?profileId=]
      Exams/
        ExamSession.razor              ← Submitted state expanded: toggle + review component
        ExamList.razor                 ← "View all →" link on each profile card
      Questions/
        ListQuestions.razor            ← pagination (25/page)
    Infrastructure/
      Migrations/
        ..._AddSelectedOptionIndicesToAttemptAnswer.cs  ← new migration

tests/
  ExamSimulator.Web.UnitTests/
    Attempts/
      ExamAttemptAnswerTests.cs        ← SelectedOptionIndices tests
  ExamSimulator.Web.FunctionalTests/
    AttemptReviewTests.cs              ← /attempts/{id} access tests
    HistoryTests.cs                    ← /history page tests
```

---

## Phases

---

### Phase 1: Schema Extension (#66)

**Changes:**
- Add `SelectedOptionIndices` (`IReadOnlyList<int>?`) to `ExamAttemptAnswer` domain class.
- Configure `HasConversion` JSON serialisation in `ExamSimulatorDbContext`.
- Update `ExamSession.Submit()` to populate `SelectedOptionIndices` from `_answers`, with Matching normalisation to canonical target indices.
- Add EF migration `AddSelectedOptionIndicesToAttemptAnswer`.

**Definition of Done:** migration applies cleanly; submit persists selected indices; existing NULL rows cause no runtime errors.

---

### Phase 2: Shared Review Component + Submitted State (#67)

**Changes:**
- Create `Components/AttemptReview.razor` accepting `List<ReviewRow>`.
- Move the existing per-question review markup from `ExamSession.razor` into the new component verbatim; add null-handling for `SelectedIndices` and deleted questions.
- Replace the inlined review in `ExamSession.razor`'s `Submitted` state with `<AttemptReview Rows="_reviewRows" />`.
- Add `BuildReviewRows()` helper and `_reviewRows` field populated at submit time.
- Add "Retake" button and "View history →" link to the `Submitted` state.

**Definition of Done:** submitting an exam behaves identically to today; Retake returns to Configuring with filters intact; "View history →" navigates to `/history?profileId=…`.

---

### Phase 3: Attempt Review Page (#68)

**Changes:**
- Create `Features/Attempts/AttemptReview.razor` at `/attempts/{AttemptId:guid}`.
- Load `ExamAttempt` with answers from DB, verify `UserId`, load question dictionary.
- Build `List<ReviewRow>` from DB data; pass to `<AttemptReview>` component (expanded by default).
- Handle deleted questions and NULL `SelectedOptionIndices` gracefully.

**Definition of Done:** navigating to `/attempts/{id}` shows the full breakdown; another user's attempt shows "not found"; deleted questions show placeholder.

---

### Phase 4: History Page (#69)

**Changes:**
- Create `Features/Attempts/History.razor` at `/history`.
- Implement `[SupplyParameterFromQuery] ProfileId` filter.
- Profile dropdown changes URL via `NavigationManager`.
- Add `View all →` link to each profile card in `ExamList.razor`.

**Definition of Done:** `/history` lists all attempts; `?profileId=` filters correctly; "View all →" on exam list navigates to filtered history; "Review" links open the review page.

---

### Phase 5: Question List Pagination (#70)

**Changes:**
- Add `_page`, `_totalCount`, `PageSize = 25` state to `ListQuestions.razor`.
- Change `ApplyFilters()` to use `CountAsync()` + `Skip/Take`.
- Reset `_page = 1` on any filter change.
- Add pagination bar below the table.

**Definition of Done:** 470 questions split across 19 pages; filters reset to page 1; Prev/Next disabled at boundaries; "Showing X–Y of Z" updates correctly.

---

### Phase 6: Tests (#71)

**Changes:**
- `ExamAttemptAnswerTests.cs` — unit tests for `SelectedOptionIndices`
- `AttemptReviewTests.cs` — functional tests for `/attempts/{id}` access control
- `HistoryTests.cs` — functional tests for `/history` and `?profileId=` filter

**Definition of Done:** all new tests pass; total test count increases from 119.
