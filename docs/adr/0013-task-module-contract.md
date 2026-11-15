# 0013. Task module contract

## Context

Tasks are code: adding or changing one means a commit and a deploy. There are three task types (code review, AI assistant, live coding), and more will come after MVP 1. A task must render in the `candidate` app for the candidate and in the `staff` app for the interviewer ([0003](0003-repository-and-frontend-layout.md)). Its responses are stored as `jsonb` ([0004](0004-database-and-ef-core.md)) and pushed live ([0006](0006-real-time-channel.md)). Old sessions must stay meaningful after a task changes, and a broken new version must be easy to take out of use without a deploy.

## Decision

### Task types own the UI; tasks are content

A **task type** ships the code:
- the content and response models
- one candidate UI and one review UI
- response validation
- optional rubric suggestions
- optional candidate endpoints

A **task** is content only and is rendered by its type's UI. A task that needs a genuinely different UI becomes a new task type.

### Authoring

One folder per task version in the `HireLab.Tasks` project, embedded as resources:

```
src/Tasks/<task-id>/v<N>/
  task.json          metadata, rubric, reference answer, interviewer notes, type-specific content
  description.md     content files referenced from task.json
  ...
```

`task.json` carries:

| Field | Meaning |
|---|---|
| `id` | Stable kebab-case slug |
| `version` | Integer |
| `draft` | Optional. A draft is hidden outside Development and excluded from the lock file. |
| `taskType` | Key of the task type that renders it |
| `title`, `summary` | Shown in the task bank |
| `tags`, `level`, `estimatedMinutes` | Filters and planning |
| `referenceAnswer` | Markdown. What a strong answer contains; it may point at files and lines. |
| `interviewerNotes` | Markdown. Hints for a stuck candidate and follow-up questions. Staff only. |
| `rubric` | Criteria, see below |
| `content` | Deserialised into the task type's content model |

Anything a task type needs for its *logic* belongs in its content model, not in the reference answer. Examples: the planted secrets the AI assistant detects, or the files a live-coding editor starts with.

The TaskBank module loads and validates every task at startup and fails fast on an invalid one. A test does the same in CI.

### Rubric

A rubric is a list of criteria. Every criterion has an `id`, `title`, `description` and one of two types:

| Type | Scored as | Extra fields |
|---|---|---|
| `scale` | 0–4 | `weight`; optional description per level |
| `check` | Yes / No | `expected` (`yes` or `no`); optional `critical` |

- The overall score is the weighted average of the `scale` criteria only.
- Checks form a separate pass/fail list. A failed `critical` check is highlighted above the score.
- Any criterion can be marked "not assessed", which excludes it.
- A task type may **suggest** a value. For example, the AI-assistant type pre-fills the data-leak check from its detection; the interviewer confirms or overrides it.
- More criterion types can be added later.

### Versions

- **Every published version is immutable once on `main`.** A committed `tasks.lock.json` holds a content hash per published version. A test fails when an existing version's hash changes, and a script adds new versions to the lock. Changing a task means adding `v<N+1>`.
- **Active version.** TaskBank stores an optional active version per task in its own table. With no row, the newest published version is active. The admin can switch it at runtime, for example back to v1 when v2 turns out to be broken.
- **What a switch does.** Pending session tasks pinned to another version are re-pinned to the active one with a fresh snapshot. Current and Done session tasks keep their version, so a response never changes the task under it.
- **Drafts by environment.** The option `TaskBank:IncludeDrafts` is true in Development and false elsewhere.

| | Development (`IncludeDrafts` on) | Production (off) |
|---|---|---|
| Task bank shows | Drafts and every published version, with "Draft" / "vN" badges | Only the active version of each task |
| Interviewers can queue | Any of them | Only the active version |
| Drafts in the lock file | No | No |

### Sessions pin and snapshot when a task is queued

Adding a task to a queue pins its version and copies a snapshot of its content, reference answer, interviewer notes and rubric into the session task. Old sessions stay self-contained even if a version later disappears from the code.

### Responses

- A response is one `jsonb` document per session task. Its shape is the task type's response model, with a `schemaVersion`.
- The candidate app autosaves the whole document: debounced about 1.5 s, and also on blur, on tab hide and before "next task".
- Only the candidate writes, so the last write wins. The server increments a `revision`, which drives the `ResponseUpdated` event.
- Server-generated parts, such as AI-assistant prompts and replies, are appended by the backend through a Sessions contract.
- Only the latest state is kept. PostHog replay (HLB-11) covers how the candidate got there.

### Backend contract

Each task type is a project, `HireLab.TaskTypes.<Name>`, discovered through DI. A sketch, not final signatures:

```csharp
public interface ITaskType
{
    string Key { get; }
    Type CandidateContentType { get; }
    Type StaffContentType { get; }
    Type ResponseType { get; }
    int ResponseSchemaVersion { get; }
}

public interface ITaskType<TCandidateContent, TStaffContent, TResponse> : ITaskType
{
    Result ValidateResponse(TResponse response);
    IReadOnlyList<CriterionSuggestion> SuggestScores(TStaffContent content, TResponse response);
}

public interface ITaskTypeEndpoints
{
    void MapCandidateEndpoints(RouteGroupBuilder group);
}
```

- A task type's content model is split into a **candidate part** and a **staff-only part**. The candidate API returns only the candidate part: never the reference answer, interviewer notes, rubric or staff-only content.
- A test asserts that the candidate OpenAPI document contains none of those fields.
- Optional candidate endpoints are mapped under `/api/candidate/tasks/<type>/…`. The AI chat proxy is the first.
- A task type changes a response only through a Sessions contract, never through the Sessions DbContext.

### Frontend contract

Each task type is a folder in the `tasks` lib with two entry points: `tasks/<type>/candidate` and `tasks/<type>/review`. Each app has a registry mapping the task type key to a lazy `loadComponent`, so the `candidate` app imports only candidate entry points. A sketch:

```ts
// candidate component
content = input.required<TCandidateContent>();
response = input.required<TResponse>();
responseChange = output<TResponse>();

// review component
content = input.required<TStaffContent>();
response = input.required<TResponse | null>();
referenceAnswer = input.required<string>();
interviewerNotes = input<string>();
live = input(false);
```

The **app shell** owns autosave, revisions, refetch on `ResponseUpdated`, reconnects and the read-only lock after Finished. Task components never call the API, with one exception: task types with their own endpoints get an injected client scoped to `/api/candidate/tasks/<type>/`.

## Consequences

- Adding a task means adding a folder: no components, no migrations. Adding a task type means adding a backend project, two frontend entry points and two registry entries.
- The task bank always offers a single, predictable version in production, and a broken version is one admin click away from rollback.
- The lock file makes "never edit a used version" mechanical. In exchange, even a typo fix in a published version means a new version.
- Snapshots duplicate content per session task. That's small, and it makes old results readable forever.
- The split content model and the OpenAPI test keep answers out of the candidate's browser. A task type author must still put each field on the right side.
- Last-write-wins autosave is correct only while the candidate is the single writer of their part of the response. Server-appended parts go through Sessions, which serialises them.

## Status

Accepted
