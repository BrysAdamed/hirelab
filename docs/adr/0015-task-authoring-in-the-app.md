# 0015. Task authoring in the app

## Context

[0013](0013-task-module-contract.md) made tasks code: content folders in the repo, with a lock file guarding published versions. Because the UI belongs to the task *type*, a task is only content, and content doesn't need a deploy. Also, the repo is public, so real interview tasks could never live in it. Only showcase tasks could. Authoring tasks in the app removes both limits.

This ADR supersedes the **Authoring** and **Versions** sections of 0013 and the "no task editor" line in the vision. The rest of 0013 stands: UI per task type, rubric, snapshots, responses, the backend and frontend contracts.

## Decision

### The database is the source of truth

Tasks and their versions live in the `taskbank` schema. Task *types* stay code.

### Seed packages

The folder format from 0013 (`src/Tasks/<task-id>/v<N>/` with `task.json` and content files) becomes an **import format** for showcase tasks. [0030](0030-seed-task-packages.md) defines the final format, the import and the lock.
- The migration worker imports the packages as **system tasks**: Global, owned by no team, read-only.
- The import is idempotent by (slug, version). It never overwrites an existing version, and it fails if an existing version's content hash differs.
- `tasks.lock.json` still guards the seed packages in CI.
- `TaskBank:ImportSeedTasks` (on by default) controls the import in every environment.

### Ownership and visibility

- Every task belongs to one team (the author's), except system tasks.
- **Visibility** is `Team` or `Global`, set by the manager.
  - `Team`: only the owning team sees the task.
  - `Global`: every team sees and can queue it. Only the owning team can edit or publish it.
- The tasks' query filter is "own team OR Global". It is the one exception to [0009](0009-authorisation-model.md)'s team filter, and the auth sweep covers it.
- Switching Global → Team hides the task from other teams. Their existing session tasks keep working, because they hold snapshots.

### Duplicate and hide

- **Duplicate** creates a draft copy owned by the current team and records its source (`duplicatedFrom`).
- Each team has a list of **hidden** Global tasks; only the manager can change it.
  - Duplicating a task hides its source automatically.
  - The manager can also hide any Global task by hand and unhide it at any time.
  - Hidden tasks are reachable through a "Show hidden" filter.
- A duplicate shows "Based on *<task>* vN". When the source publishes a newer version, the duplicate shows a notice. Nothing is merged automatically.

### Roles

| Action | Interviewer | Manager |
|---|---|---|
| Create a task, edit a draft, duplicate, preview | ✓ | ✓ |
| Publish, switch the active version, change visibility | | ✓ |
| Hide / unhide Global tasks, archive, delete | | ✓ |

The admin has no team and no part in tasks. The active-version switch from 0013 moves to the manager.

### Version lifecycle

- A task has versions, and **at most one draft at a time**. "Edit" on a published task creates the draft as a copy of the active version.
- **Publishing** makes the draft immutable and, by default, the active version. The manager can untick "make active".
- **Rollback** is the active-version switch. A switch re-pins Pending session tasks (as in 0013).
- Two people saving the same draft: optimistic concurrency (`xmin`). The second save gets a 409: "Someone else changed this draft. Reload?"
- Every version records who created it and who published it, and when.

### Drafts and preview

- **Preview** in the editor renders the task type's candidate component with the draft content and a throwaway local response. No session is involved.
- Drafts can't be queued in production. In Development, `TaskBank:IncludeDrafts` still allows queuing them for testing.

### Content

- Text only (markdown, code files, JSON) in `jsonb`, at most about 1 MB per version.
- No binary assets in MVP 1. Images could come later through Blob storage.

### Deleting

A task that has never been queued can be hard-deleted, drafts included. A task that has been queued is **archived**: hidden from the bank, while sessions keep their snapshots. Only the manager deletes or archives.

### Identity

Tasks get a `Guid` v7 id like everything else. System tasks keep their slug as an external key for idempotent import.

### Editors

- **Generic part**, in the staff app, shared by every type: metadata, visibility, rubric editor, reference answer, interviewer notes.
- **Type part**: each task type adds a third frontend entry point, `tasks/<type>/editor`. It edits only the type's `content` (both its candidate and staff-only parts), with input `content`, output `contentChange`, and the backend's validation errors to display.
- **Dedicated editors from the start.** Every task type ships its editor with the type, never a generic JSON fallback.
- The backend's `ITaskType` gains `ValidateContent`. The same validation runs on save, on publish and on seed import.

A task type is now: a backend project, plus `candidate`, `review` and `editor` entry points.

## Consequences

- Real interview tasks can live in the app without ever touching the public repo. The showcase still lives in the repo as seed packages.
- Changing a task no longer needs a deploy. Publishing is a manager's decision instead of a code review.
- Immutability of published versions is enforced by the domain, not by a lock file, except for seed packages.
- Each task type costs more up front, because it needs an editor. Since types are added one at a time, that cost never piles up.
- Global tasks plus per-team hiding make the tasks filter more complex than other team-owned data. The auth sweep has to cover it explicitly.
- Duplicates diverge from their source by design. The newer-version notice is the only link back.

## Status

Accepted
