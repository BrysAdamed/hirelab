# 0020. Teams and user lifecycle

## Context

[0009](0009-authorisation-model.md) scopes data by team, and the vision describes an interviewer as belonging to "one manager". If a team were identified by its manager's user id, replacing a manager would orphan every job position, session and evaluation the team owns, or force us to rewrite their `TeamId`. People also leave: staff accounts have to stop working without losing the history they authored. [0007](0007-staff-authentication.md) already fixes how people get accounts (invite links) and how sessions end (security stamp, validated every 5 minutes).

We need to settle what a team is, how many admins and managers there can be, who may deactivate whom, and how a team changes hands.

## Decision

**A team is its own entity** with a `TeamId` (Guid v7) and a name, stored in the Identity module. Team-owned aggregates in other modules store the `TeamId`, never a manager's `UserId`. A team is created when a manager invite for a new team is redeemed, so revoked invites leave no empty teams. Teams are not deleted.

**Users.** A user has an email (the login), a display name, a role (`Admin`, `Manager`, `Interviewer`), a `TeamId` and an active flag.

| Rule | Enforced by |
|---|---|
| Exactly one admin, with no team | At most one: a unique partial index on `role = 'Admin'`. At least one: the development seed locally, the bootstrap invite in Azure (HLB-39). |
| Managers and interviewers belong to exactly one team; the admin to none | A check constraint: `(role = 'Admin') = (team_id IS NULL)` |
| At most one active manager per team | A unique partial index on `team_id` where `role = 'Manager' AND is_active` |

The database enforces these rules, so two concurrent requests can't break them. Handlers still check first, to return a readable `409` instead of a constraint violation.

**Deactivation instead of deletion.** Users are never hard-deleted; their notes, evaluations and decisions keep pointing at them.
- The admin can deactivate and reactivate any user except themselves.
- A manager can deactivate and reactivate the interviewers of their own team.
- Interviewers can't deactivate anyone.
- A deactivated user can't log in, and their open sessions end at the next security stamp validation (at most 5 minutes). The sign-in manager refuses inactive users both at login and at that validation.
- Reactivating a manager is refused while their team has another active manager.

**Every change that affects access updates the security stamp**: deactivation, reactivation, a role change, moving an interviewer to another team, and a password reset. The new stamp ends existing sessions within the validation window, so a reassigned interviewer loses the old team's data at once ([0009](0009-authorisation-model.md)).

**Handover.** To give a team a new manager, the admin deactivates the old manager (the team then shows "No manager") and invites a new manager into the existing team. The team keeps its id, so its data stays with it. The admin can also move interviewers between teams.

**Staff sessions** ([0007](0007-staff-authentication.md)): the cookie slides after 8 hours of inactivity and ends 7 days after login whatever the activity. The login time travels in the cookie's authentication properties.

## Consequences

- Changing a manager never touches data in other modules.
- A team can exist without an active manager. Screens and invites have to handle "No manager".
- "Exactly one admin" depends on the bootstrap path. If the only admin is locked out, recovery is the bootstrap job's manual option (HLB-39), not a second admin.
- Deactivated accounts stay in the database with their personal data (email, display name). Erasing them would need a separate anonymisation step later.
- Expected rule violations come back as `409`; a race that slips past the handler check is still stopped by the database.

## Status

Accepted
