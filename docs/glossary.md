# Glossary

Use these terms in code, API, UI and tickets. When you introduce a new domain term, add it here.

| Term | Meaning |
|---|---|
| **Job position** | An opening the team is hiring for: a title and the Markdown sections description, requirements (required), nice-to-haves and interviewer notes. Its state is `Open` or `Closed`; a closed position is read-only. The manager creates, edits and deletes positions (delete only while open and without candidates); interviewers read them. In code: `JobPosition`. |
| **Version** | The number an editable aggregate is read with (PostgreSQL `xmin`). An edit sends it back; if someone else saved in between, the edit is refused with "changed by someone else, reload". |
| **Candidate** | A person applying to exactly one job position. The same person applying again is a new candidate. Has a full name, an optional email and phone, up to 3 profile links and a number. The manager and interviewers of the team add and edit candidates while the position is open. Their personal data lives only in Positions; other modules hold the candidate id (ADR 0023). In code: `Candidate`. |
| **Candidate number** | A candidate's place in their position's sequence (1, 2, 3 …), given when they are added and never changed or reused. |
| **Candidate #N** | How an anonymised candidate is shown, N being their candidate number. Otherwise a candidate is shown by full name. In code: `Candidate.DisplayName`. |
| **Attachment** | A file on a candidate's profile. The CV is an attachment. PDF, DOCX, TXT, MD, PNG or JPG, at most 10 MB, at most 10 per candidate. The manager and interviewers of the team upload and delete them while the position is open and download them any time; anonymisation deletes them (ADR 0005, ADR 0024). In code: `Attachment`. |
| **Anonymisation** | Removing a candidate's personal data: contact details are cleared, attachments are deleted and the candidate is shown as "Candidate #N" everywhere. Who did it and when is recorded (`AnonymisedBy`/`AnonymisedAt`). Closing a job position anonymises all its candidates. Notes, scores and task results stay. It is irreversible. |
| **Anonymise candidate** | Anonymising a single candidate while their position stays open. |
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
| **Interviewer notes** | Markdown on a task or a job position with hints for a stuck candidate and follow-up questions. Staff only. |
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
| **Deactivation** | Switching a user off instead of deleting them: they can't log in and their sessions end within 5 minutes. The admin deactivates anyone but themselves; a manager deactivates their own interviewers. It also revokes the pending invite and reset links the user issued and the reset links for them, so a departing manager's links neither create accounts nor take any over. Reversible by reactivation, which is refused for a manager while their team has another active manager. |
| **Handover** | Giving a team a new manager: the admin deactivates the old manager, the team shows "No manager", and the admin invites a new manager into the same team. |
| **Reassignment** | The admin moving an interviewer to another team. Their sessions end within 5 minutes, after which they see only the new team's data. Their pending reset links are revoked; invites are not affected. Managers are not reassigned; a team changes manager by handover. |
| **Security stamp** | A random value on each user that changes on deactivation, role or team change and password reset. Sessions issued before the change end at the next check, at most 5 minutes later. |
| **Invite link** | A one-time link, valid for 72 hours, that creates a user account in a given role and team. The admin invites a manager into a new team (created only when the link is redeemed) or into an existing team without an active manager; a manager invites interviewers into their own team. Issuing a new invite for the same email revokes the old one, but only if the issuer manages it: a pending invite from someone else (the admin, or another team's manager) blocks the new one until it is revoked or expires. In code: `Invite`. |
| **Bootstrap invite** | The admin invite link (valid for 24 hours) that the migration job issues while no admin exists, and writes to its log. A manual run with `--issue-admin-link` replaces it, or issues a reset link for the admin once they exist. It never creates a second admin. |
| **Invite state** | The state of an invite or reset link: `Pending` (can be redeemed), `Used`, `Revoked` or `Expired`. Only a pending link works. In code: `OneTimeLinkState`. |
| **Redemption** | Opening an invite link and setting a password: the account is created and the invitee is logged in. |
| **Reset link** | A one-time link, valid for 24 hours, that lets an existing user set a new password. The admin generates it for a manager, a manager for their own interviewers, and it is sent by hand. Redeeming it clears the lockout and ends the user's existing sessions; the user then logs in with the new password. Generating a new link revokes the user's previous unused one; deactivating or reassigning the user, or deactivating the issuer, revokes it too. In code: `ResetLink`. |
