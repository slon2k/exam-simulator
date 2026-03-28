---
name: start-phase
description: "Start an iteration phase: create branch, implement per plan, build, test, commit, push, and generate a PR summary. Use when: starting a phase, implement phase, begin iteration phase, start iteration."
agent: agent

You are implementing a phase of the exam-simulator project.

## Inputs

- **iteration**: ${{ input:iteration }}  *(e.g. 9)*
- **phase**: ${{ input:phase }}  *(e.g. 1)*
- **branch**: ${{ input:branch }}  *(e.g. iteration-9/phase-1-schema-extension)*
- **issue**: ${{ input:issue }}  *(GitHub issue number(s) this phase closes, e.g. 73)*

## Workflow

Follow these steps in order. Do not skip any step.

### Step 1 — Read the plan

Read `docs/iteration-${{ input:iteration }}-plan.md`.
Locate the section for **Phase ${{ input:phase }}** and extract:
- The list of changes required
- The Definition of Done

### Step 2 — Create the branch

```
git checkout -b ${{ input:branch }}
```

### Step 3 — Implement

Apply every change described in the Phase ${{ input:phase }} section of the plan.
- Read existing files before editing them.
- Make all independent edits in parallel using `multi_replace_string_in_file`.
- Do not add features, refactors, or comments beyond what the plan specifies.

### Step 4 — Build

```
dotnet build ExamSimulator.slnx
```

Fix any compiler errors before proceeding. Do not proceed if the build fails.

### Step 5 — Run tests

```
dotnet test ExamSimulator.slnx
```

All tests must pass. Fix failures caused by the Phase ${{ input:phase }} changes before proceeding.

### Step 6 — Commit and push

Stage and commit all changed files with a conventional commit message that references the issue:

```
git add -A
git commit -m "<type>: <description> (closes #${{ input:issue }})"
git push -u origin ${{ input:branch }}
```

Use the appropriate type prefix: `feat` for new behaviour, `fix` for corrections, `refactor` for restructuring, `test` for test-only changes.

### Step 7 — PR summary

Output a PR summary in Markdown using the repository's PR template as a guide:

```
## Summary
<bullet list of what changed>

## Why
<reason per the iteration plan>

## Validation
- [x] `dotnet build ExamSimulator.slnx` — passed
- [x] `dotnet test ExamSimulator.slnx` — N tests passed
- [ ] Manual smoke test (if UI changed)

## Scope Check
- [x] This PR targets only one active issue (#${{ input:issue }})
- [x] No unrelated refactors
- [x] Docs updated if behavior changed
```

Fill in actual test counts and specifics from the build/test output.
