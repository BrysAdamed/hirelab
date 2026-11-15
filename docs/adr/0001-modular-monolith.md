# 0001. Modular monolith with five modules

## Context

HireLab has two kinds of users, staff (admin, manager, interviewer) and candidates, and a handful of clearly separate areas: users and teams, job positions, the task bank, interview sessions and evaluation. It runs on a personal Azure subscription, so every extra deployable costs money and operational effort. The team is one person.

Separate candidate and staff services would isolate the candidate side, but would add network calls, two deployments and double the hosting cost, for a load of a few interviews a day.

## Decision

**One deployable.** A single ASP.NET Core API host serves both SPAs' APIs. Isolation between the candidate and staff sides is done with separate route groups and auth schemes (see [0002](0002-backend-internal-style.md)), not separate processes.

**Five modules:**

| Module | Owns |
|---|---|
| Identity | Users, roles, teams, invite links |
| Positions | Job positions, candidates, attachments, anonymisation |
| TaskBank | The task catalogue and task registration. Read-only, defined in code. |
| Sessions | Interview sessions, task queue, session tasks, candidate links, responses |
| Evaluation | Rubric scores, notes, the manager's decision |

**One project per module** (`HireLab.Modules.<Name>`) plus a small `HireLab.Modules.<Name>.Contracts` project. Everything in a module is `internal` except its registration entry point. Other modules may reference only `.Contracts`. Architecture tests enforce this.

**Communication between modules:**
- Synchronous: interfaces and DTOs in `.Contracts`, for example `ITaskCatalog.Get(taskId, version)`.
- Reactions: in-process integration events, also defined in `.Contracts`, dispatched after the originating transaction commits.
- No outbox or message bus for now. One is added when a reaction must not be lost.

**Database boundaries.** One database, one schema per module (`identity`, `positions`, `taskbank`, `sessions`, `evaluation`). No foreign keys or joins across schemas; modules refer to each other's data by id and fetch it through `.Contracts`.

**Domain modelling depth matches complexity.** Rich aggregates where the rules live: Sessions (session states, task queue rules) and Positions (closing, anonymisation). Thin models elsewhere: TaskBank is code, Identity builds on ASP.NET Core Identity.

## Consequences

- One build, one container, one deployment, one database to pay for.
- Module boundaries are enforced by the compiler (`internal`) and by architecture tests rather than by the network. They stay honest only as long as those tests run in CI.
- A module could be extracted into a service later: it already has its own schema and talks to the others only through `.Contracts`.
- In-process events are lost if the process crashes between the commit and the dispatch. That's acceptable until a reaction becomes critical; then we add an outbox.
- Cross-module reads cost a call instead of a join. Screens that combine data from several modules compose it in the handler.

## Status

Accepted
