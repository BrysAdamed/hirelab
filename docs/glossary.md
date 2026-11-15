# Glossary

Use these terms in code, API, UI and tickets. When you introduce a new domain term, add it here.

| Term | Meaning |
|---|---|
| **Job position** | An opening the team is hiring for. It holds requirements, nice-to-haves and other information. Its state is `Open` or `Closed`. |
| **Candidate** | A person applying to exactly one job position. The same person applying again is a new candidate. |
| **Attachment** | A file on a candidate's profile. The CV is an attachment. |
| **Anonymisation** | What closing a job position does to its candidates: contact details are replaced with "Candidate #N" and attachments are deleted. Notes, scores and task results stay. It is irreversible. |
| **Task** | One interview exercise, defined in code. It has an id, version, title, tags, level, estimated minutes, candidate content, reference answer, rubric, candidate UI and review UI. |
| **Task type** | The kind of task and the UI that comes with it: code review, AI assistant, live coding. |
| **Task bank** | The catalogue of all tasks, filterable by tag and level. |
| **Level** | A task's difficulty: `Junior`, `Mid` or `Senior`. |
| **Tag** | A free label on a task, such as `csharp`, `cqrs`, `security` or `role:qa`. |
| **Reference answer** | What a strong candidate would produce for a task. It is shown next to the candidate's response. |
| **Rubric** | The scoring criteria of a task, defined with the task. |
| **Interview session** | One meeting with a candidate. It has a task queue and a state: `Planned` → `Live` → `Finished`. |
| **Task queue** | The ordered tasks of a session. Tasks that haven't started can be added, removed or reordered while the session is Live. |
| **Session task** | One task in a queue. It stores the task id, version and a snapshot of the content shown, plus its state: `Pending`, `Current` or `Done`. |
| **Candidate link** | An unguessable URL that gives the candidate access to a session while it is Live. |
| **Response** | Everything a candidate produced for a session task: comments, files, prompts. It is saved continuously. |
| **Evaluation** | One interviewer's rubric scores and notes for a candidate's session. Its author and the manager can see it. |
| **Decision** | The manager's final hire / no-hire for a candidate, with a summary. |
| **Admin / Manager / Interviewer** | User roles. See `docs/vision.md#roles`. |
| **Team** | A manager and the interviewers assigned to them. |
| **Invite link** | A one-time link with an expiry that creates a user account in a given role and team. |
