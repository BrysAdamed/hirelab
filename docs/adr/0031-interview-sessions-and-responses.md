# 0031. Interview sessions and responses

## Context

Epic HLB-6 builds interview sessions: an interviewer or manager creates a session from a candidate profile, queues tasks from the task bank, sends the candidate a link, runs the session live and watches the responses arrive. Several earlier records left pieces of it open: [0004](0004-database-and-ef-core.md) stores responses as `jsonb`, [0006](0006-real-time-channel.md) fixes the hubs and thin events, [0008](0008-candidate-access.md) the candidate link and cookie, [0026](0026-closing-positions-and-anonymisation.md) the unfinished-session contract, [0028](0028-task-type-plug-in-contract.md) lazy re-pinning and [0029](0029-task-storage.md) the snapshot query. The epic's refinement (2026-11-16) settled the rest. This record keeps the whole design in one place so the stories HLB-50 to HLB-55 follow it.

## Decision

### The session aggregate

`InterviewSession` (table `sessions.interview_sessions`) is team-owned (`ITeamOwned`, [0009](0009-authorisation-model.md)) and holds:

| Field | Meaning |
|---|---|
| `CandidateId` | The candidate, from `Positions.Contracts`. No name or contact detail is copied ([0023](0023-candidate-personal-data-stays-in-positions.md)). |
| `Number` | The session's place among its candidate's sessions, shown as "Session N". `max + 1` when planned, unique per candidate. A session has no title, and a candidate may have any number of sessions. |
| `ScheduledAt` | Optional. Set when planning and changed while `Planned`. |
| `State` | `Planned` → `Live` → `Finished`. There is no `Cancelled`: a session that won't happen is deleted while `Planned`. A finished session can't be reopened. |
| `CreatedBy` | Who planned it. Any manager or interviewer of the team controls the session; the creator is only recorded. |
| `QueueRevision` | Raised by every queue change. It changes the session row, so two concurrent queue changes can't both be saved (`xmin`, [0004](0004-database-and-ef-core.md)), and it is the revision queue events carry. |

**Session tasks** (table `sessions.session_tasks`) belong to the aggregate and are loaded with it; they have no `TeamId` of their own and a `Team` query filter through their session, so they are never read without it. Each has a `Position` (0, 1, 2 … without gaps), a state `Pending` → `Current` → `Done`, the `TaskId`, and the pinned version with its **snapshot**: version id and number, task type key, content schema version, title, summary, candidate content and staff content (two `jsonb` columns, as in the task bank), reference answer, interviewer notes and rubric (`jsonb`). A task appears at most once in a queue, and a queue holds at most 30 tasks.

### Lifecycle rules

| Action | Allowed when |
|---|---|
| Plan (create) | The candidate is in the team, not anonymised and their position is open. |
| Reschedule | `Planned` |
| Delete (with its queue) | `Planned` |
| Add, remove, reorder tasks | `Planned`; while `Live` only `Pending` tasks are added, removed or moved, and they stay after the `Current` and `Done` ones (HLB-51). |
| Go Live | `Planned`, at least one queued task, the candidate not anonymised and the position open (HLB-51). The first task becomes `Current`. |
| Mark the current task done | `Live`: it becomes `Done` and the next `Pending` task `Current`; after the last one no task is `Current`, the session stays `Live` and the candidate sees a closing screen (HLB-51). |
| Finish | Any time while `Live`. The `Current` task becomes `Done`; `Pending` tasks stay and are shown as "not reached". A session never finishes on its own (HLB-51). |

Running a session (HLB-51):

- Going Live, marking done and finishing are `POST`s on the session: `/go-live`, `/finish` and `/tasks/{sessionTaskId}/done`. Mark done names the task it completes and is refused (`InterviewSession.TaskNotCurrent`) when that task isn't `Current` any more, so a double click or two interviewers clicking at once never skips a task.
- Starting tasks are always a prefix of the queue: going Live and mark done start the first `Pending` task, and added tasks go to the end. A live reorder must therefore list the `Done` and `Current` tasks first, in their places (`InterviewSession.TaskStarted` otherwise), the same error as removing a started task. A finished session's queue can't change at all (`InterviewSession.Finished`).
- Going Live, mark done and a finish that completes the `Current` task raise `QueueRevision`, since task states are part of the queue.
- The staff session page reads one task's staff material (reference answer, interviewer notes) from its snapshot through `GET /api/staff/sessions/{id}/tasks/{sessionTaskId}`; the later stories add the response there.

The guards on the candidate read `ICandidateDirectory`, whose `CandidateSummary` carries `IsAnonymised` and `IsJobPositionClosed`; there is no cross-schema join. Closing a position anonymises its candidates, so the position check comes first and gives the clearer error. As [0026](0026-closing-positions-and-anonymisation.md) notes, a close racing a plan or a go-live is not locked across schemas; the go-live guard catches a close that happened before it.

**Unfinished sessions.** `IUnfinishedSessionsQuery` answers from `interview_sessions`: a session that isn't `Finished` counts. It runs inside the Positions request, so the team filter applies, which is right because a candidate and their sessions are always in the same team.

### Queueing and re-pinning

Queueing a task reads `IActiveTaskVersionQuery` ([0029](0029-task-storage.md)) as the current user, so only a task in their task bank can be queued, and stores the snapshot. The staff UI's picker is the task bank list with its filters.

A `Pending` session task follows its task's active version until it starts ([0028](0028-task-type-plug-in-contract.md)). Sessions re-checks it when the queue is opened (`GET /api/staff/sessions/{id}`, which saves the new pins; if a concurrent queue change wins, the pins are re-checked at the next open) and when the task becomes `Current` (HLB-51). A changed active version re-pins with a fresh snapshot. A task that is no longer in the bank (archived, or no longer visible to the team) or whose task type is no longer registered keeps its pin; Sessions checks the registry before asking the snapshot query, which would throw for an unregistered type, and logs the session, session task and task ids. `Current` and `Done` tasks never change their pin.

### Responses

A **response** is everything a candidate produced for one session task. It is stored in its own table, one row per session task, as a `jsonb` document with the task type's `ResponseSchemaVersion` and a `Revision` (HLB-53):

- The candidate app autosaves the whole response, debounced, sending the revision it last saw. The server validates it with the task type's `ValidateResponse`, stores it and raises the revision; a stale revision is refused with `409`, after which the client refetches and reapplies.
- Only the `Current` task's response can be written, and only while the session is `Live`. `Done` tasks are read-only.
- Re-opening the link continues from the stored response.
- HLB-6 owns response storage, autosave, both hubs and the live staff view; a task type supplies only its components and models.

### Candidate link

The candidate link follows [0008](0008-candidate-access.md) (HLB-52):

- It is available from `Planned` (256-bit token, only its SHA-256 hash stored) and can be copied any time before `Finished`. While `Planned` it opens a waiting page that switches on its own when the session goes Live.
- It stops working when the session is `Finished`. Regenerating it revokes the old token, the cookies issued from it and their hub connections.
- Creating a link, regenerating it and going Live are refused for an anonymised candidate or a closed position.
- Candidate endpoints return only the current task's candidate content, never the queue or its length; the OpenAPI leak test stays green.

### Real time

Both hubs follow [0006](0006-real-time-channel.md) (HLB-54). Sessions raises integration events after commit (queue changed with `QueueRevision`, session state changed, response changed with its revision), and the API's `Realtime` component turns them into thin hub messages. Staff see candidate presence; any number of team members can watch one session.

The events are records in `Sessions.Contracts` (HLB-51): `TaskQueueChanged(SessionId, QueueRevision)` after every queue change, task state changes included, and `SessionStateChanged(SessionId)` after going Live and finishing. Handlers enqueue them on the shared kernel's `IIntegrationEventQueue`; the unit of work dispatches them in process after a successful save and drops them when the command fails. Re-pinning on open doesn't raise the queue revision and raises no event: the task's id and place don't change, and each viewer re-pins when they open the queue.

## Consequences

- One aggregate owns the queue, so ordering and the per-state rules live in one class, and `QueueRevision` makes concurrent queue edits fail cleanly instead of interleaving.
- Opening a session is a read with a possible write. That keeps re-pinning lazy without a background job, at the cost of one snapshot query per `Pending` task on every open.
- A session never depends on the task bank after a task starts: snapshots are copies, so archiving or editing a task changes nothing that was already shown to a candidate.
- Snapshots duplicate task content per session task. With at most 30 tasks per session and content of at most 1 MB, that is cheap.
- Deleting is the only way to drop a planned session, so the history of `Live` and `Finished` sessions is never lost.

## Status

Accepted
