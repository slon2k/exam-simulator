# Iteration 7 Plan: Question Import

## Current State (End of Iteration 6)

- All five question types: `SingleChoice`, `MultipleChoice`, `Ordering`, `BuildList`, `Matching`. ✅
- Full question admin CRUD and exam session with post-exam review. ✅
- ASP.NET Core Identity: cookie auth, `Admin` role, dev admin seeding. ✅
- 81 tests (66 unit + 15 functional), all passing. ✅
- Staging deploy complete. ✅
- No bulk import mechanism — questions must be added one-by-one through the admin UI. ❌

## Goals

1. Define a JSON import schema that can represent all five question types.
2. Add a `/questions/import` admin page with a file upload control.
3. Parse and validate the uploaded JSON; show a preview before committing.
4. Bulk-insert valid questions; skip any that already exist (idempotent via optional `id` field).
5. Cover the new functionality with unit tests (validation) and functional tests (import endpoint).

## Key Decisions

### JSON Import Schema

The import file is a single JSON object with a top-level `examProfileId` string and a `questions` array.

```json
{
  "examProfileId": "az-204",
  "questions": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "type": "SingleChoice",
      "difficulty": "Medium",
      "prompt": "What is Azure App Service?",
      "options": ["A PaaS offering", "An IaaS offering", "A SaaS offering"],
      "correctOptionIndices": [0],
      "topicTag": "app-service",
      "explanation": "App Service is a managed PaaS platform.",
      "matchingTargets": null
    }
  ]
}
```

**Field rules:**

| Field | Required | Notes |
|---|---|---|
| `examProfileId` | Yes | Must match an existing `ExamProfile.ProfileId` |
| `questions[].id` | No | Omit to auto-generate; provide a GUID for idempotent re-import |
| `questions[].type` | Yes | One of the `QuestionType` enum values |
| `questions[].difficulty` | Yes | One of: `Easy`, `Medium`, `Hard` |
| `questions[].prompt` | Yes | Non-empty string |
| `questions[].options` | Yes (except `Ordering`) | Non-empty list for choice/matching types |
| `questions[].correctOptionIndices` | Yes (except `Ordering`, `Matching`) | Valid indices into `options` |
| `questions[].topicTag` | No | Optional tag string |
| `questions[].explanation` | No | Optional markdown explanation |
| `questions[].matchingTargets` | Only for `Matching` | Pool of ≥ 2 target strings; each premise selects from this pool via dropdown; the same target may appear in multiple correct pairings |

### Deduplication Strategy

- If a question `id` is provided and a question with that `id` already exists in the database, the record is **skipped** (not updated).
- If `id` is omitted, a new GUID is always generated and the question is always inserted.
- The import result summary reports counts: inserted, skipped, and failed.

### UI Flow

1. Admin navigates to `/questions/import`.
2. Selects a `.json` file via a file input.
3. Clicks **Upload & Validate** — the file is parsed; a preview table shows each question (type, prompt, status: valid / invalid / duplicate).
4. If all valid (or warnings acknowledged), clicks **Import** — bulk inserts are committed.
5. Success/error toast is shown; admin is redirected to `/questions`.

### Error Handling

- If the JSON file is malformed (parse error), show an error message and block import.
- If individual questions fail validation, mark them as invalid in the preview table and block import until resolved or removed.
- If the `examProfileId` does not match any existing profile, block import at the file level.

## Target State (End of Iteration 7)

```
src/
  ExamSimulator.Web/
    Features/
      Questions/
        Import/
          QuestionImportDto.cs         ← DTO: ImportFile + ImportQuestion records
          QuestionImportValidator.cs   ← validates each ImportQuestion
          QuestionImportService.cs     ← parses JSON, runs validation, performs bulk insert
          ImportQuestions.razor        ← /questions/import admin page (file upload + preview)
tests/
  ExamSimulator.Web.UnitTests/
    Questions/
      QuestionImportValidatorTests.cs  ← unit tests for validation rules
  ExamSimulator.Web.FunctionalTests/
    ImportQuestionsTests.cs            ← functional tests: unauthenticated redirect, non-admin forbidden, valid import
```

## Phases

---

### Phase 1: DTOs and JSON Schema

**Closes:** #53

**Changes:**
- Create `QuestionImportDto.cs` under `Features/Questions/Import/`:
  - `QuestionImportFileDto` record: `ExamProfileId` (string) + `Questions` (list of `QuestionImportItemDto`).
  - `QuestionImportItemDto` record: all fields from the schema above (`Id?`, `Type`, `Difficulty`, `Prompt`, `Options`, `CorrectOptionIndices`, `TopicTag`, `Explanation`, `MatchingTargets`).
- `System.Text.Json` deserialization with `JsonPropertyName` attributes; camelCase contract.

**Definition of Done:** DTO records exist and the JSON sample in this plan deserializes correctly.

---

### Phase 2: Validation and Import Service

**Closes:** #54

**Changes:**
- Create `QuestionImportValidator.cs`:
  - Validates each `QuestionImportItemDto` against the field rules table above.
  - Returns a `ValidationResult` with a list of error messages per item.
- Create `QuestionImportService.cs`:
  - Accepts a `Stream` (file content).
  - Uses `JsonSerializer.DeserializeAsync` to parse.
  - Resolves `ExamProfile` by `examProfileId`; returns error if not found.
  - For each question: runs validator; checks for existing `id` in the database.
  - Returns an `ImportPreview` describing valid/invalid/duplicate items.
  - `CommitAsync(ImportPreview)` bulk-inserts all valid, non-duplicate items via `ExamSimulatorDbContext`.
- Register service in DI.

**Definition of Done:** Service can parse the sample JSON, validate items, check for duplicates, and insert new questions.

---

### Phase 3: Admin Import Page

**Closes:** #55

**Changes:**
- Create `ImportQuestions.razor` at route `/questions/import`:
  - `@attribute [Authorize(Roles = "Admin")]`
  - `InputFile` component for `.json` file selection.
  - On upload: call `QuestionImportService` to get `ImportPreview`.
  - Render a preview table: one row per question showing index, type, prompt (truncated), and status badge (Valid / Invalid / Duplicate).
  - Show aggregate counts (X valid, Y invalid, Z duplicate).
  - If any invalid: show error details; disable the **Import** button.
  - On **Import** click: call `CommitAsync`; show success/error notification; navigate to `/questions`.
- Add an "Import" link/button on the `/questions` list page.

**Definition of Done:** Admin can upload a JSON file, see the preview, and successfully import questions. Invalid files are rejected with clear messages.

---

### Phase 4: Tests

**Closes:** #56

**Changes:**
- `QuestionImportValidatorTests.cs` (unit):
  - Valid single-choice item passes.
  - Item with empty prompt fails.
  - Item with out-of-range `correctOptionIndices` fails.
  - `Matching` item without `matchingTargets` fails.
  - `Matching` item with mismatched `matchingTargets` count fails.
  - Item with unknown `type` fails.
- `ImportQuestionsTests.cs` (functional):
  - `GET /questions/import` when unauthenticated → 302 redirect to login.
  - `GET /questions/import` when authenticated without Admin role → 403 Forbidden.
  - `GET /questions/import` when authenticated as Admin → 200 OK.

**Definition of Done:** All existing tests still pass; new tests pass.
