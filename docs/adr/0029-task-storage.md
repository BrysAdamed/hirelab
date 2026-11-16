# 0029. Task storage

## Context

[0015](0015-task-authoring-in-the-app.md) moved tasks into the `taskbank` schema and [0028](0028-task-type-plug-in-contract.md) fixed the storage decisions that follow from the task type contract (two content columns, a content schema version per version). HLB-47 builds the storage itself. Seed import (HLB-48), the task bank (HLB-49), the editor (HLB-22) and Sessions (HLB-6) all build on it, so the shape of the aggregate, the visibility filter, the validation entry point and the snapshot contract have to be settled once.

## Decision

**One aggregate.** `InterviewTask` (table `tasks`) owns its `TaskVersion`s (table `task_versions`, cascade delete). The class isn't called `Task` because that clashes with .NET's `Task` in every async file. A task has `TeamId` (null for a system task), `Visibility`, `IsSystem`, `Slug` (system tasks only, unique), `TaskTypeKey`, `IsArchived` with who and when, `CreatedBy` and `xmin`. A version has its number, state, metadata, `text[]` tags with a GIN index, reference answer, interviewer notes, rubric (`jsonb`), `candidate_content` and `staff_content` (`jsonb`), `content_schema_version`, `content_hash`, and who created and published it, and when.

**Database invariants.** Check constraints keep a system task Global, teamless and slugged and a team task teamed and slugless, and a version published exactly when it has `published_at`. Unique indexes give at most one draft per task and one row per version number, so two concurrent "start draft" requests can't both win.

**Active version.** `tasks.active_version_id` is the manager's choice; when it is null the newest published version is active. A draft is never active. A system task never gets a choice, so it always uses its newest version. Publishing "make active" clears the choice; publishing without it pins the version that was active. The rule lives twice, kept equal by tests: in memory (`InterviewTask.ActiveVersion`) and as a query (`TasksInBank`, which also drops archived tasks).

**Visibility filter.** A named query filter `Visibility` on `InterviewTask`: the current user has a team, and the task is that team's or Global. `TaskVersion` has the same named filter through its task, so a version can't be read without its task being visible. Both are built on `TeamQueryFilter.CurrentTeamOf`, so the admin, candidates and code outside a request see nothing. The auth sweep lists both entities as having their own team filter and fails if either loses it.

**One validation.** `TaskVersionValidator.Validate(taskTypeKey, TaskVersionDetails)` runs the generic rules (title 1–120, summary 1–1000, level, estimated minutes 1–180, tags normalised and limited, reference answer, rubric rules, content ≤ 1 MB) and then the type's `ValidateContent`, and reports all failures together as one `ValidationError`. On success it returns a `ValidatedTaskVersion`: the normalised details, the type's content schema version and the content hash. The aggregate creates versions only from a `ValidatedTaskVersion`, so seed import and the editor can't skip validation.

**Content hash.** SHA-256 over the canonical JSON (written with `TaskTypeJson.Options`, content minified) of the task type, content schema version and every normalised detail. Formatting changes don't change it; any change of content or metadata does. Seed import compares it to detect an edited seed version.

**Schema mismatch.** Reading a version whose content schema version differs from its registered type's throws. A task whose type is no longer registered makes the snapshot query throw; the bank (HLB-49) leaves such tasks out with a warning instead.

**Out-of-order discriminators.** `jsonb` reorders object keys, so the `type` discriminator of a rubric criterion can come after other properties. `TaskTypeJson.Options` allows that (`AllowOutOfOrderMetadataProperties`).

**Contract.** `IActiveTaskVersionQuery.FindAsync(TaskId)` in `TaskBank.Contracts` returns a `TaskVersionSnapshot` of the task's active version (version id, number, type key, content schema version, title, summary, both content parts, reference answer, interviewer notes, rubric) for a task in the current user's bank, or `null`.

## Consequences

- Every write path goes through one validator and the aggregate; a handler that loads a task must include its versions.
- The active version needs a correlated subquery; with a handful of versions per task that is cheap, and the bank's list query reuses it.
- The visibility filter on versions adds an `EXISTS` on `tasks` to every version query.
- Changing what the content hash covers changes every hash, which would make seed import report existing seed versions as edited. Such a change needs a migration of the stored hashes.

## Status

Accepted
