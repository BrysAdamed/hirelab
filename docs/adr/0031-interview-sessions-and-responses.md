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

How HLB-53 settled it:

- **Storage.** `Response` (table `sessions.responses`) is its own small aggregate, not part of the session: an autosave every second must not load the queue or touch the session's `xmin`, so it never conflicts with queue changes. Its key is the session task id (a foreign key to `session_tasks`, deleted with it); it also stores the `SessionId`, which gives it the same `Team` query filter as session tasks. Columns: `content` (`jsonb`), `response_schema_version`, `revision`. There is no row before the first save, which the APIs show as no response and revision 0.
- **Writing.** `PUT /api/candidate/session/response` with `{ sessionTaskId, response, revision }`. The session task id is in the body because candidate routes carry no ids (the OpenAPI contract test). The handler reads the task through `CandidateSessions` (by both cookie ids) and refuses a session that isn't `Live` (`409 InterviewSession.NotLive`), a task that isn't `Current` (`409 InterviewSession.TaskNotCurrent`) and a task of another session (`404`); a finished session is already refused by the cookie check (`401 CandidateSession.Ended`). A task type that is no longer registered gives `409 Response.TaskTypeNotRegistered`.
- **Validation and size.** The raw JSON may be at most 1 MB (`400`, field `response`); the request body is capped a little above that. The task type's `ValidateResponse` runs next, then the response is read into the type's response model and written back with `TaskTypeJson.Options`, so the stored document is the canonical one the type validated, with the type's `ResponseSchemaVersion`.
- **Revisions.** A save names the revision it was based on and gets the next one back. A different revision, or losing a race with another tab (the revision is the row's concurrency token; two first saves meet on the primary key), is `409 Response.StaleRevision`. A save that races a mark done may still land just before the task becomes `Done`; that is the candidate's last edit, so we don't lock against it.
- **Event.** A save raises `ResponseUpdated(SessionId, SessionTaskId, Revision)` after commit: ids and the revision, never content.
- **No content in logs.** The request and command records override `ToString`, and the handler logging writes only the request name and error code.
- **Candidate app.** `ResponseAutosave` saves the whole response one second after the last change, one save at a time, each naming the revision the previous one returned, and saves at once when the page is hidden. A stale refusal refetches the interview, takes the saved response and shows "Updated in another tab"; any other `409`, `401` or `404` refetches the interview, which moves on to the next task or the end. A `400 Antiforgery.InvalidToken` gets a new token and retries once; a failed save is retried after 5 seconds. Changes not yet saved when the current task changes are dropped, since the server refuses them. The 5-second poll also takes a newer saved response when nothing is unsaved, so a second tab follows the first.
- **Staff view.** `GET /api/staff/sessions/{id}/tasks/{sessionTaskId}` adds the candidate and staff content, the response and its revision. The session page renders the type's `review` component through `hl-review-task-outlet` (tasks lib) for every task state, with `live` while the task is `Current` and the session `Live`; a type without one shows the reference answer and notes alone. Until the staff hub (HLB-54) refetches on `ResponseUpdated`, a "Refresh response" button reloads it.

### Candidate link

The candidate link follows [0008](0008-candidate-access.md) (HLB-52):

- It is available from `Planned` (256-bit token, only its SHA-256 hash stored) and can be copied any time before `Finished`. While `Planned` it opens a waiting page that switches on its own when the session goes Live.
- It stops working when the session is `Finished`. Regenerating it revokes the old token, the cookies issued from it and their hub connections.
- Creating a link, regenerating it and going Live are refused for an anonymised candidate or a closed position.
- Candidate endpoints return only the current task's candidate content, never the queue or its length; the OpenAPI leak test stays green.

How HLB-52 settled it:

- **Storage.** The link is part of the session aggregate: an owned `CandidateLink` (id, token hash, created at and by) in `interview_sessions`, with a unique index on the hash. A session has at most one link, and history isn't kept. Creating and regenerating are one staff endpoint, `POST /api/staff/sessions/{id}/candidate-link`, allowed while `Planned` or `Live`; two concurrent regenerations conflict on the session's `xmin`.
- **Shown once.** Only the hash is stored, so the link can't be shown again, as with one-time links ([0021](0021-one-time-links.md)). The endpoint returns the link's path once and the staff app shows it with a copy button. The session page shows when the current link was created; to send it again, the interviewer regenerates it. We chose this over storing the token (or encrypting it) because a copy of a working link in the database is exactly what [0008](0008-candidate-access.md) rules out, and regenerating costs the interviewer one click.
- **Regenerating** gives the link a new `CandidateLinkId`. The cookie carries the id of the link it was exchanged for, so cookies from the old link stop working on their next request. The handler raises `CandidateLinkRevoked(SessionId, CandidateLinkId)` with the old id, for the candidate hub to drop its connections (HLB-54).
- **Exchange.** The candidate app's route `/s/{token}` hands the token to a service and replaces the address with `/s/` before anything is sent. It then gets an antiforgery token (`GET /api/candidate/access/antiforgery`) and posts the token in the body to `POST /api/candidate/access` (anonymous, rate limited, antiforgery checked like every unsafe method). `Planned` and `Live` give the candidate cookie (claims `sessionId`, `candidateLinkId`) and a new `XSRF-TOKEN` for the candidate. An unknown token, a regenerated link and a deleted session answer `404 CandidateLink.Invalid` alike; a finished session answers `409 CandidateSession.Ended` and gives no cookie.
- **Every request.** The candidate cookie scheme validates the principal through `ICandidateAccessValidator` (`Sessions.Contracts`): one lookup by both ids. A finished or deleted session or a regenerated link rejects and deletes the cookie, and the `401` carries `CandidateSession.Ended` or `CandidateLink.Invalid`, so the app can say "This interview has ended" or "This link doesn't work". The same check will guard the candidate hub.
- **Team filter.** Candidates have no team, and [0009](0009-authorisation-model.md) scopes them by their session. Their reads live in one class, `CandidateSessions`, marked `ICandidateScope` (SharedKernel). It bypasses only the `Team` filter and finds a session only by the token's hash or by both of the cookie's ids. The architecture test allows `IgnoreQueryFilters` in types marked `ICandidateScope`, besides the migration worker and system jobs.
- **Candidate view.** `GET /api/candidate/session` returns the stage, `Waiting` (Planned), `CurrentTask` (Live with a Current task) or `Closing` (Live, every task done), and only at `CurrentTask` the current task's id, task type key and candidate content. Not its title, which is written for staff, and nothing about other tasks. The page renders the type's `candidate` component through `hl-candidate-task-outlet` (tasks lib), the same outlet as the staff candidate preview, with a response that isn't saved yet. HLB-53 adds the stored response and its revision to that task and a write endpoint that names the session task, so a write for a task that is no longer Current is refused.
- **Following the session.** Until the candidate hub exists (HLB-54) the page polls `GET /api/candidate/session` every 5 seconds through `InterviewUpdates`, which the hub's thin events will replace. The current task's component is created again only when the task's id changes.
- **The token in the URL.** The token is in the path, as [0008](0008-candidate-access.md) fixes, so the request for the candidate app's page carries it. Traces redact it (`/s/{token}`), the candidate app sends no `Referer`, and the app removes it from the address bar at once.

### Real time

Both hubs follow [0006](0006-real-time-channel.md) (HLB-54). Sessions raises integration events after commit (queue changed with `QueueRevision`, session state changed, response changed with its revision), and the API's `Realtime` component turns them into thin hub messages. Staff see candidate presence; any number of team members can watch one session.

The events are records in `Sessions.Contracts` (HLB-51): `TaskQueueChanged(SessionId, QueueRevision)` after every queue change, task state changes included, and `SessionStateChanged(SessionId)` after going Live and finishing, and `ResponseUpdated(SessionId, SessionTaskId, Revision)` after a response is saved (HLB-53). Handlers enqueue them on the shared kernel's `IIntegrationEventQueue`; the unit of work dispatches them in process after a successful save and drops them when the command fails. Re-pinning on open doesn't raise the queue revision and raises no event: the task's id and place don't change, and each viewer re-pins when they open the queue.

## Consequences

- One aggregate owns the queue, so ordering and the per-state rules live in one class, and `QueueRevision` makes concurrent queue edits fail cleanly instead of interleaving.
- Opening a session is a read with a possible write. That keeps re-pinning lazy without a background job, at the cost of one snapshot query per `Pending` task on every open.
- A session never depends on the task bank after a task starts: snapshots are copies, so archiving or editing a task changes nothing that was already shown to a candidate.
- Snapshots duplicate task content per session task. With at most 30 tasks per session and content of at most 1 MB, that is cheap.
- Deleting is the only way to drop a planned session, so the history of `Live` and `Finished` sessions is never lost.

## Status

Accepted
