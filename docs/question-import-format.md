# Question Import JSON Format

Questions can be bulk-imported via the admin page at `/questions/import` by uploading a JSON file (max 5 MB).

## Top-level structure

```json
{
  "examProfileId": "az-204",
  "questions": [ ... ]
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `examProfileId` | `string` | Yes | ID of the exam profile to attach all questions to |
| `questions` | `array` | Yes | One or more question objects |

---

## Question object

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "MultipleChoice",
  "difficulty": "Medium",
  "prompt": "Which of the following...",
  "options": ["Option A", "Option B", "Option C"],
  "correctOptionIndices": [0, 2],
  "topicTag": "Storage",
  "explanation": "Option A is correct because...",
  "matchingTargets": null
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `string` (UUID) | Optional | Stable GUID for idempotent re-import. If omitted, a new GUID is generated each time. |
| `type` | `string` (enum) | Yes | See [Question types](#question-types) |
| `difficulty` | `string` (enum) | Yes | `Easy`, `Medium`, or `Hard` |
| `prompt` | `string` | Yes | The question text |
| `options` | `string[]` | Yes | At least 2 options; all must be non-empty |
| `correctOptionIndices` | `int[]` | Yes* | 0-based indices into `options`; semantics depend on type |
| `topicTag` | `string` | Optional | Free-text topic label |
| `explanation` | `string` | Optional | Shown to the learner after answering |
| `matchingTargets` | `string[]` | Matching only | Right-hand column of pairs; see [Matching](#matching) |

> \* Required for all types. See per-type rules below.

---

## Question types

### `SingleChoice`

The learner picks exactly one answer.

- `correctOptionIndices`: exactly **1** index
- `matchingTargets`: not used

```json
{
  "type": "SingleChoice",
  "options": ["A", "B", "C", "D"],
  "correctOptionIndices": [2]
}
```

---

### `MultipleChoice`

The learner picks one or more answers.

- `correctOptionIndices`: **1 or more** indices (unique, in range)
- `matchingTargets`: not used

```json
{
  "type": "MultipleChoice",
  "options": ["A", "B", "C", "D", "E"],
  "correctOptionIndices": [0, 2, 4]
}
```

---

### `Ordering`

The learner arranges all options in the correct order.

- `correctOptionIndices`: a **full permutation** — one index per option, all indices present exactly once
- `matchingTargets`: not used

```json
{
  "type": "Ordering",
  "options": ["Step A", "Step B", "Step C"],
  "correctOptionIndices": [1, 2, 0]
}
```

The example encodes the correct order as: Step B → Step C → Step A.

---

### `BuildList`

The learner selects a subset of options and places them in order.

- `correctOptionIndices`: **2 or more** indices (unique, in range); must be a **proper subset** of options (not all of them)
- `matchingTargets`: not used

```json
{
  "type": "BuildList",
  "options": ["A", "B", "C", "D", "E"],
  "correctOptionIndices": [0, 2, 4]
}
```

---

### `Matching`

The learner pairs each premise (left column) with a target (right column) using a dropdown per premise. The same target may be selected for multiple premises (repetition is allowed), so the targets act as a pool rather than a 1-to-1 set.

- `options`: the **premises** (left column)
- `matchingTargets`: the **pool of targets** (right column); must contain **at least 2 entries**; all must be non-empty; may contain more or fewer entries than `options`
- `correctOptionIndices`: one index per premise — `correctOptionIndices[i]` is the 0-based index into `matchingTargets` that premise `i` pairs with; the same index may appear multiple times (repetition)

```json
{
  "type": "Matching",
  "options": ["Premise 1", "Premise 2", "Premise 3"],
  "matchingTargets": ["Target A", "Target B"],
  "correctOptionIndices": [1, 0, 1]
}
```

The example pairs Premise 1 → Target B, Premise 2 → Target A, Premise 3 → Target B (Target B used twice).

---

## Deduplication

If a question with the same `id` already exists in the database it is silently skipped (shown as **Duplicate** in the import preview). Re-uploading the same file is safe.

---

## Full example

See [sample-import-az204.json](sample-import-az204.json) for a complete example with all fields.
