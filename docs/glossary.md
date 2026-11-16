# Glossary

Use these terms in code, API, UI and tickets. When you introduce a new domain term, add it here.

| Term | Meaning |
|---|---|
| **Job position** | An opening the team is hiring for. It holds requirements, nice-to-haves and other information. Its state is `Open` or `Closed`. |
| **Candidate** | A person applying to exactly one job position. The same person applying again is a new candidate. |
| **Attachment** | A file on a candidate's profile. The CV is an attachment. |
| **Anonymisation** | What closing a job position does to its candidates: contact details are replaced with "Candidate #N" and attachments are deleted. Notes, scores and task results stay. It is irreversible. |
| **Task** | One interview exercise, authored in the app. It has an owning team, a visibility, versions, a task type, title, summary, tags, level, estimated minutes, content, reference answer, interviewer notes and rubric. Its UI comes from its task type. |
| **Task version** | One revision of a task (`v1`, `v2` …). Published versions are immutable; a change means a new version. |
| **Draft** | The one unpublished version a task may have. Editable by the team; can be previewed, but queued only in Development. |
| **Active version** | The task version the task bank offers. By default the newest published one; the manager can switch it. |
| **Visibility** | Whether a task is seen by its owning team only (`Team`) or by every team (`Global`). Only the owning team edits it. |
| **System task** | A showcase task imported from a seed package in the repo. Global, owned by no team, read-only. |
| **Seed package** | A task version stored as a folder in `src/Tasks/` and imported into the database as a system task. |
| **Duplicate** | A team's draft copy of another task. It remembers its source, which is then hidden from the team's bank. |
| **Hidden task** | A Global task a team's manager has hidden from that team's bank. Reachable through "Show hidden". |
| **Task type** | The kind of task and the code that comes with it: content and response models, a candidate UI, a review UI and an editor. Code review, AI assistant, live coding. |
| **Task bank** | The catalogue of all tasks, filterable by tag and level. |
| **Level** | A task's difficulty: `Junior`, `Mid` or `Senior`. |
| **Tag** | A free label on a task, such as `csharp`, `cqrs`, `security` or `role:qa`. |
| **Reference answer** | Markdown describing what a strong answer contains. It is shown next to the candidate's response, never to the candidate. |
| **Interviewer notes** | Markdown on a task with hints for a stuck candidate and follow-up questions. Staff only. |
| **Rubric** | The scoring criteria of a task, defined with the task. |
| **Criterion** | One rubric item. A `scale` criterion is scored 0–4 and weighted; a `check` criterion is Yes / No with an expected answer. Any criterion can be "not assessed". |
| **Critical check** | A `check` criterion whose failure is highlighted above the score. |
| **Interview session** | One meeting with a candidate. It has a task queue and a state: `Planned` → `Live` → `Finished`. |
| **Task queue** | The ordered tasks of a session. Tasks that haven't started can be added, removed or reordered while the session is Live. |
| **Session task** | One task in a queue. When queued, it pins a task version and stores a snapshot of its content, reference answer, interviewer notes and rubric. Its state is `Pending`, `Current` or `Done`. |
| **Candidate link** | An unguessable URL that gives the candidate access to one session. Before Live it shows a waiting page; after Finished it stops working. The interviewer can regenerate it, which revokes the old one. |
| **Response** | Everything a candidate produced for a session task: comments, files, prompts. It is saved continuously. |
| **Evaluation** | One interviewer's rubric scores and notes for a candidate's session. Its author and the manager can see it. |
| **Decision** | The manager's final hire / no-hire for a candidate, with a summary. |
| **Admin / Manager / Interviewer** | User roles. See `docs/vision.md#roles`. There is exactly one admin, who has no team. |
| **User** | A staff account: email (the login), display name, role, team and an active flag. Candidates are not users. |
| **Team** | An entity with its own id and name that owns job positions, sessions and evaluations. It has at most one active manager and any number of interviewers, and keeps its data when its manager changes. |
| **Team-owned** | Data that belongs to one team (job positions, sessions, evaluations …). Only that team's manager and interviewers see it; the admin sees none. In code: `ITeamOwned`. |
| **Deactivation** | Switching a user off instead of deleting them: they can't log in and their sessions end within 5 minutes. The admin deactivates anyone but themselves; a manager deactivates their own interviewers. Reversible by reactivation. |
| **Handover** | Giving a team a new manager: the admin deactivates the old manager, the team shows "No manager", and the admin invites a new manager into the same team. |
| **Security stamp** | A random value on each user that changes on deactivation, role or team change and password reset. Sessions issued before the change end at the next check, at most 5 minutes later. |
| **Invite link** | A one-time link with an expiry that creates a user account in a given role and team. |
| **Reset link** | A one-time link with an expiry that lets a user set a new password. A manager or admin generates it and sends it by hand. |
