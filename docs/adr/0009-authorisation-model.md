# 0009. Authorisation model

## Context

Staff have three fixed roles. Managers and interviewers work within one team and must only see that team's job positions, candidates, sessions and evaluations. The admin manages users and must see no candidate data. Evaluations are visible only to their author and the manager. A check that a handler can forget is a data leak.

## Decision

**Roles as policies.** Endpoints require role policies (`Admin`, `Manager`, `ManagerOrInterviewer`) at the route-group or endpoint level, on top of the group's authentication requirement. There is no permissions table.

**Team scoping by global query filter.**
- Every team-owned aggregate root (job position, interview session, evaluation …) stores a `TeamId`.
- Each module's DbContext has an EF global query filter on `TeamId`, fed by the current user.
- The admin has no team, so the filter returns no candidate data for them by construction.

**Bypassing the filter** (`IgnoreQueryFilters`) is allowed only in the migration worker and in system jobs such as anonymisation and the orphan sweep. An architecture test enforces this.

**Finer rules stay explicit in handlers.** Examples: an evaluation is visible only to its author and the manager; only the manager records a decision. These checks return a `Forbidden` or `NotFound` `Error` ([0002](0002-backend-internal-style.md)).

**Candidates** are scoped by their cookie's `sessionId` claim ([0008](0008-candidate-access.md)), not by teams.

## Consequences

- Forgetting the team check in a new query is impossible. Forgetting a finer rule is still possible, so those need tests.
- When an admin reassigns an interviewer to another team, the interviewer loses access to the old team's data at once.
- A global query filter is invisible when reading a handler; the convention has to be learned. This rule is also listed in `CLAUDE.md`.
- Cross-team reporting would need an explicit, tested bypass.

## Status

Accepted
