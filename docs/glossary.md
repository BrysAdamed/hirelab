# Glossary

Use these terms in code, API, UI and tickets. When you introduce a new domain term, add it here.

| Term | Meaning |
|---|---|
| **Job position** | An opening the team is hiring for. It holds requirements, nice-to-haves and other information. Its state is `Open` or `Closed`. |
| **Candidate** | A person applying to exactly one job position. The same person applying again is a new candidate. |
| **Attachment** | A file on a candidate's profile. The CV is an attachment. |
| **Anonymisation** | What closing a job position does to its candidates: contact details are replaced with "Candidate #N" and attachments are deleted. Notes, scores and task results stay. It is irreversible. |
| **Task** | One interview exercise, defined as content in code. It has an id, versions, a task type, title, summary, tags, level, estimated minutes, content, reference answer, interviewer notes and rubric. Its UI comes from its task type. |
| **Task version** | One immutable revision of a task (`v1`, `v2` …). A change to a published task means a new version. |
| **Draft** | A task version that isn't published yet. Visible only in Development. |
| **Active version** | The task version the task bank offers in production. By default the newest published one; the admin can switch it. |
| **Task type** | The kind of task and the code that comes with it: content and response models, a candidate UI and a review UI. Code review, AI assistant, live coding. |
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
| **Admin / Manager / Interviewer** | User roles. See `docs/vision.md#roles`. |
| **Team** | A manager and the interviewers assigned to them. |
| **Invite link** | A one-time link with an expiry that creates a user account in a given role and team. |
| **Reset link** | A one-time link with an expiry that lets a user set a new password. A manager or admin generates it and sends it by hand. |
