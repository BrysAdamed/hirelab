# 0030. Seed task packages

## Context

[0015](0015-task-authoring-in-the-app.md) turned 0013's task folders into an import format for showcase tasks: the migration worker imports them as system tasks, idempotently, and `tasks.lock.json` guards them in CI. [0028](0028-task-type-plug-in-contract.md) dropped `draft` from the format and split content into a candidate and a staff part, and [0029](0029-task-storage.md) gave every task version a content hash. HLB-48 builds the import and the guard, which leaves open the exact `task.json` format, how content files get into it, which hash detects an edited seed, and how the migration worker learns the task types.

## Decision

### Packages

- One folder per task version: `src/Tasks/<slug>/v<N>/`. The slug is kebab-case; `N` counts from 1 without gaps or leading zeros. The folder is the package's only identity: `task.json` doesn't repeat the slug or the version, so copying `v1` to `v2` can't leave a stale number behind.
- The files are embedded in the `HireLab.Tasks` assembly (`src/Tasks/HireLab.Tasks.csproj`) as resources named `seed/<slug>/v<N>/<path>`. Only the migration worker and tests reference it; the migration worker passes it to `TaskBankModule(Assembly, resourcePrefix)`.
- Content is text only (0015). A package holds `task.json` and the files it references, nothing else: an unreferenced file fails, so a typo in a reference can't leave the intended file silently unused.

### `task.json`

0013's table minus `id`, `version` and `draft`, with 0013's `content` split as in 0028. Field names are camelCase and parsed with `TaskTypeJson.Options`, so an unknown field (a typo, or `content`) fails.

| Field | Required | Maps to |
|---|---|---|
| `taskType` | yes | the task type key; it must be registered |
| `title`, `summary` | yes | `TaskVersionDetails.Title`, `Summary` |
| `level` | yes | `Junior`, `Mid` or `Senior` |
| `estimatedMinutes` | yes | 1–180 |
| `tags` | no | defaults to none |
| `referenceAnswer` | yes | Markdown |
| `interviewerNotes` | no | Markdown |
| `rubric` | yes | `{ "criteria": [ { "type": "scale" \| "check", ... } ] }` as in 0028 |
| `candidateContent`, `staffContent` | yes | the task type's two content models |
| `draft` | never | rejected: seeds are always published (0028) |

**File references.** Anywhere in `task.json`, an object `{ "$file": "<path>" }` with no other field is replaced by that file's text before parsing: UTF-8, BOM removed, CRLF turned into LF. The path is relative to the package folder, uses `/` and has no empty, `.` or `..` segment. So a long reference answer, a markdown description in the candidate content or a code file a code-review task shows lives in its own file:

```json
{
  "taskType": "code-review",
  "title": "Review a made-up invoice service",
  "summary": "Find the bugs in a small fake service.",
  "level": "Mid",
  "estimatedMinutes": 30,
  "tags": ["csharp", "review"],
  "referenceAnswer": { "$file": "reference-answer.md" },
  "rubric": { "criteria": [ { "type": "scale", "id": "bugs-found", "title": "Bugs found", "description": "...", "weight": 3 } ] },
  "candidateContent": { "files": [ { "path": "InvoiceService.cs", "text": { "$file": "content/InvoiceService.cs" } } ] },
  "staffContent": { "plantedBugs": [ ... ] }
}
```

The details then go through the one task version validation (0029): the generic rules and the type's `ValidateContent`.

### Import

`IModuleMigrator.BootstrapAsync` of TaskBank runs the import in every environment, after all migrations ([0022](0022-first-admin-bootstrap.md)), unless `TaskBank:ImportSeedTasks` is `false`; then it reads nothing.

- It reads and validates every package before writing, and saves everything in one transaction. Any failure throws, the migration worker exits with code 1, and the API, which waits for the worker, doesn't start.
- By (slug, version): a missing task is created as a system task with v1, a missing version is added with the next number (which must equal `N`), and a stored version is never written to.
- Failures: a file outside a package folder, a missing `task.json`, a `draft` field, invalid JSON or an unknown field, a bad or missing file reference, an unreferenced file, a version gap, an unregistered task type, a task type that differs from the stored task's, a validation error (listed by field and message), and a stored version whose content hash differs.
- Errors and logs name the package (`<slug>/v<N>`) and the broken rule, never the content.

### Two hashes

Both detect an edited published seed, at different times, so they hash different things on purpose.

- **The content hash in the database** (`task_versions.content_hash`) is 0029's: the canonical JSON of the validated details and content. The import re-validates each package and compares that hash with the stored version's. Since it covers what was imported rather than the files, moving text between `task.json` and a referenced file or reformatting the JSON is not a change, and it works the same for app-authored versions.
- **The lock hash in `tasks.lock.json`** covers the package files themselves: SHA-256 (lowercase hex) over the files in ordinal order of their path relative to the package folder; per file its path in UTF-8, a NUL byte, the content's length as an 8-byte big-endian number and the content with CRLF turned into LF. `.gitattributes` checks `src/Tasks/**` out with LF, and the hash normalises anyway, so the lock is the same on Windows and Linux. A file hash needs no task types and no database, so it is a cheap, strict guard in review: any byte change to a published package shows in the pull request's lock diff or fails CI.

### The lock guard

`tests/HireLab.Tasks.Tests` reads every embedded package, validates it against the shipped task types and compares the lock hashes with `src/Tasks/tasks.lock.json`, a JSON object of `"<slug>/v<N>": "<hash>"` in ordinal order. It fails when a locked version changed or disappeared, or a version isn't locked yet. It runs in CI with every other test (`dotnet test --solution HireLab.slnx`).

`eng/update-tasks-lock.ps1` runs it with `HIRELAB_UPDATE_TASKS_LOCK=1`, which adds the new versions to the lock first. It never changes or removes a locked hash, so the update can't hide an edited version.

### One task type catalog

`TaskTypeCatalog.AddTaskTypes()` moves from the API into `src/TaskTypes/HireLab.TaskTypes.Catalog`, which the API, the migration worker and the lock guard reference. A new task type is added there once (a project reference and an `AddTaskType<T>()` call), and the three can't disagree about which types exist.

## Consequences

- Adding a showcase task is a folder plus `eng/update-tasks-lock.ps1`; fixing one is a new `v<N+1>` folder. The import picks it up at the next deployment and the new version becomes the system task's active version.
- A bad seed package blocks a deployment at the migration worker instead of breaking the task bank at runtime.
- Changing what either hash covers needs care: a change to the content hash makes every imported seed version look edited (0029), and a change to the lock hash needs the lock regenerated in the same pull request.
- A content schema change in a task type makes the stored seed versions' content hash differ on the next import; the type's seed packages then need new versions, as 0028 already requires for stored tasks.
- The `$file` property name is reserved: a task type's content model can't have a `$file` field.

## Status

Accepted
