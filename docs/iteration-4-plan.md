# Iteration 4 Plan: Ordering Questions + Randomisation + Explanation + Markdown

## Current State (End of Iteration 3)

- `ExamProfile` domain entity with slug PK, admin CRUD. ✅
- FK from `Question.ExamProfileId` to `ExamProfile` with `DeleteBehavior.Restrict`. ✅
- Learner exam-taking flow: `/exams` list → `/exams/{ProfileId}` session → in-memory scoring. ✅
- Nav links for "Take an Exam" and "Exam Profiles". ✅
- 47 unit tests + 6 functional tests, all passing. ✅
- `Ordering` question type — deferred. ❌
- Markdown rendering for prompt and explanation — deferred. ❌
- Explanation not shown in results review — deferred. ❌
- Question order randomisation — deferred. ❌
- Exam attempt persistence — deferred (requires authentication, planned for Iteration 5). ❌

## Goals

1. Implement the `Ordering` question type across domain, admin UI, and exam session.
2. Randomise question order each time a session starts.
3. Show explanation in the results review panel after submission.
4. Render question prompts and explanations as Markdown using **Markdig**.

## Key Decisions

- **Ordering answer model**: `CorrectOptionIndices` is a permutation of `[0..n-1]` — the correct ordering of the options. Invariant: `Count == Options.Count`, all indices distinct, all in range.
- **Ordering admin UX**: option entry order = correct order. Up/Down arrow buttons for reordering. No drag-and-drop (avoids JS interop complexity).
- **Ordering session UX**: Up/Down buttons to reorder options. `_answers[q.Id]` stores the user's current permutation as `List<int>`.
- **Ordering scoring**: `SequenceEqual` (order matters) instead of `HashSet.SetEquals` (used for Single/Multiple).
- **Randomisation**: `Random.Shared.NextInt64()` seed-based `OrderBy` on questions after loading in `ExamSession.razor`. Single line.
- **Explanation overlay**: in the existing per-question results review cards in `ExamSession.razor`, add the explanation block below the correct-answer summary.
- **Markdown**: `Markdig` NuGet package; `MarkdownText.razor` shared component wrapping `Markdig.Markdown.ToHtml`. Applied to prompt (during session) and explanation (in results).
- **No new DB migration** — `QuestionType` is stored as `int`; all changes are in-process.
- **Attempt persistence** — deferred to Iteration 5 (requires authentication first).

## Target State (End of Iteration 4)

```
src/
  ExamSimulator.Web/
    Domain/
      Questions/
        QuestionType.cs          ← adds Ordering = 2
        Question.cs              ← new Ordering invariant
    Features/
      Questions/
        CreateQuestion.razor     ← Ordering: Up/Down option reorder, no "correct" checkboxes
        EditQuestion.razor       ← same
      Exams/
        ExamSession.razor        ← Ordering render + scoring, randomisation, explanation panel, Markdown
    Shared/
      MarkdownText.razor         ← new shared component
tests/
  ExamSimulator.Web.UnitTests/
    Questions/
      OrderingQuestionTests.cs   ← new
```

## Phases

---

### Phase 1: Ordering Question Type

**Closes:** #29

Steps:

1. Add `Ordering = 2` to `Domain/Questions/QuestionType.cs`.

2. Add `Ordering` invariant in `Question` constructor:
   - `CorrectOptionIndices.Count == Options.Count`
   - All indices in `[0, Options.Count)`
   - All indices distinct (form a permutation)
   - No changes to the `SingleChoice` / `MultipleChoice` paths.

3. Update `Features/Questions/CreateQuestion.razor`:
   - When type = `Ordering`: render option inputs with Up ↑ / Down ↓ buttons to reorder. The saved order is the correct order.
   - Hide the "correct" checkbox column for Ordering.
   - Pass `Enumerable.Range(0, options.Count)` as `correctOptionIndices`.

4. Update `Features/Questions/EditQuestion.razor` with the same changes.

5. Update `Features/Exams/ExamSession.razor`:
   - For `Ordering` questions: render options in a reorderable list (Up/Down buttons).
   - `_answers[q.Id]` initialised to `Enumerable.Range(0, q.Options.Count).ToList()` (identity permutation = as-displayed order after randomisation).
   - Scoring: use `SequenceEqual` for `Ordering`, keep `HashSet.SetEquals` for `SingleChoice`/`MultipleChoice`.

**Verification:**

- Create an Ordering question in admin. Save. Edit. Verify options are displayed in the saved order.
- In exam session: reorder → submit wrong order → marked incorrect. Submit correct order → marked correct.
- `dotnet build` succeeds with 0 errors.

**Commit:** `feat: Ordering question type — domain invariant, admin UI, session render + scoring (#29)`

---

### Phase 2: Question Order Randomisation

**Closes:** #30

Steps:

1. In `ExamSession.razor` `OnInitializedAsync`, after loading questions:
   ```csharp
   _questions = _questions.OrderBy(_ => Random.Shared.Next()).ToList();
   ```

**Verification:**

- Run the same exam twice; confirm question order differs between sessions.

**Commit:** `feat: randomise question order on session start (#30)`

---

### Phase 3: Explanation Overlay

**Closes:** #31

Steps:

1. In `ExamSession.razor` results section, inside each per-question review card, add after the correct-answer summary:
   ```razor
   @if (q.Explanation is not null)
   {
       <div class="mt-2 text-muted fst-italic">@q.Explanation</div>
   }
   ```
   (Will be replaced with `<MarkdownText>` in Phase 4.)

**Verification:**

- Submit an exam. Explanation text appears under each question with an explanation set.
- Questions without explanation show nothing extra.

**Commit:** `feat: show explanation in results review panel (#31)`

---

### Phase 4: Markdown Rendering

**Closes:** #32

Steps:

1. Add `Markdig` package:
   ```
   dotnet add src/ExamSimulator.Web/ExamSimulator.Web.csproj package Markdig
   ```

2. Create `Features/Shared/MarkdownText.razor`:
   ```razor
   @using Markdig

   <span>@((MarkupString)_html)</span>

   @code {
       [Parameter, EditorRequired] public string Value { get; set; } = "";
       private string _html = "";

       protected override void OnParametersSet()
       {
           _html = Markdown.ToHtml(Value ?? "", new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
       }
   }
   ```

3. In `ExamSession.razor`:
   - Replace `@q.Prompt` text with `<MarkdownText Value="@q.Prompt" />`.
   - Replace the plain explanation `@q.Explanation` from Phase 3 with `<MarkdownText Value="@q.Explanation" />`.

4. Add `@using ExamSimulator.Web.Features.Shared` (or the correct namespace) to `_Imports.razor` if needed.

**Verification:**

- A question whose prompt contains `**bold**` or a code block renders correctly in the session.
- Explanation with `*italic*` renders as italic in the results panel.

**Commit:** `feat: Markdown rendering for question prompt and explanation using Markdig (#32)`

---

### Phase 5: Tests

**Closes:** #33

Steps:

1. Create `tests/ExamSimulator.Web.UnitTests/Questions/OrderingQuestionTests.cs`:
   - Valid construction: permutation of `[0, 1, 2]` accepted.
   - Rejects: `CorrectOptionIndices.Count != Options.Count`.
   - Rejects: duplicate index in permutation.
   - Rejects: index out of range.
   - Scoring check: `SequenceEqual` correctly distinguishes correct vs. wrong order (unit test on the logic, not on the component).

2. Extend `tests/ExamSimulator.Web.FunctionalTests/ExamTests.cs` to add a smoke test confirming the exam list still loads (regression check after all changes).

**Verification:**

- `dotnet test` — all existing 53 tests pass + new Ordering unit tests pass.

**Commit:** `test: Ordering question unit tests (#33)`

---

### Phase 6: Deploy to Staging

**Depends on:** All phases.

Steps:

1. Commit and push all changes.
2. Trigger the manual staging deploy workflow.
3. Smoke test: create an Ordering question, run an exam session, verify Markdown renders and explanation appears in results.
