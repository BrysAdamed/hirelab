# Vision

**HireLab** is a platform for live technical interviews. Interviewers pick tasks from a coded task bank and queue them for a candidate. The candidate opens a link and works through the tasks. The interviewer watches the results come in live and reviews them later next to a reference answer.

It is a personal portfolio project. The repo is public and the task bank holds showcase tasks only.

## Why

The live part of a software engineer interview used to look like this: we email the candidate one `.cs` file, they share their screen, read the task and talk through a code review. That setup has problems:

| Pain | Root cause |
|---|---|
| There is one task, and it is a code review only | No bank of tasks to choose from |
| A plain code review says little now that AI agents write and review code | No task type that checks how someone works with an agent |
| Impressions of a candidate fade, and after five interviews a day they mix up | Nothing records what the candidate did or what we thought of it |
| We never see whether a candidate pastes personal or confidential data into an LLM | We can't observe their prompts |
| Reading a file in an editor is nothing like a real review | The task UI doesn't resemble the tools people actually use |
| We can't adjust the interview when a candidate struggles in one area | The task is fixed before the call |

## What it does

- **Job positions.** A manager creates a job position that holds its requirements, nice-to-haves and anything else a candidate or interviewer may need. Candidates are added to a position together with their CV and other attachments.
- **Task bank.** Tasks are written in code. Each task has tags, a level (`Junior` / `Mid` / `Senior`), a reference answer, interviewer notes and a rubric. Its candidate and review UI come from its task type. Adding or changing a task means a code change and a deploy; there is no task editor in the app.
- **Interview sessions.** From a candidate's profile, an interviewer creates a session and queues tasks. When the session goes Live, the interviewer copies a link and sends it to the candidate. The candidate sees only the current task. The interviewer marks a task done to move to the next one, and can add, remove or reorder the tasks that haven't started yet.
- **Live results.** Everything the candidate produces is saved as it happens and appears in the interviewer's view: review comments, edited files, prompts sent to the AI assistant.
- **Evaluation.** Interviewers score each task against its rubric and write notes. They compare the candidate's answer with the reference answer. The manager records the final decision.
- **Data minimisation.** Closing a position anonymises its candidates: contact details are replaced and CVs are deleted. Notes, scores and results are kept.

The interview call itself still happens on Teams, and the candidate shares their screen there. HireLab records results, not video. PostHog session replay covers the screen-recording side.

## Task types

1. **Code review.** A pull-request-like UI with a diff, inline comments and a summary. The first task is a port of the original DonationRequest review.
2. **Working with an AI assistant.** A built-in assistant whose prompts pass through our backend. Task materials contain planted fake personal data and secrets, and HireLab flags them when they reach the model.
3. **Live coding.** An editor with one or more files. The interviewer sees the final state of the files. There is no code execution for now.

## Roles

| Role | Can |
|---|---|
| Admin | Manage users: create managers and reassign interviewers. Admins have no access to candidate data. There is one, seeded on first start. |
| Manager | Create and close job positions, invite interviewers to their team, see every note and score, record the final decision |
| Interviewer | Belongs to one manager. Adds candidates, runs sessions and evaluates on that manager's positions. Sees their own notes and the whole team's task results. |
| Candidate | No account. Opens a session link that works while the session is Live. |

## Releases

- **MVP 1**: everything described above, which is epics HLB-1 to HLB-11. It ends with the first deployment to Azure; until then HireLab runs only locally.
- **Later versions**: new task types, and anything else from the out-of-scope list we decide to revisit.

## Out of scope

- A task editor in the app
- Video or audio. The call stays on Teams.
- Running candidate code (could be a later epic)
- Email delivery. Invite links and candidate links are copied and sent by hand.
- Integration with an applicant tracking system
- Languages other than English
- A public demo mode

## Constraints

- Hosted on Azure in an EU region and kept cheap enough for a personal subscription
- Local accounts first, designed so SSO can be added later
- Public repo: no real candidate data, names or company material anywhere in it
