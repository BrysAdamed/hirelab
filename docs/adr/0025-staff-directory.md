# 0025. Staff directory

## Context

Modules record who did something by `UserId`: who added a candidate, who uploaded an attachment, and later who ran a session or wrote an evaluation. Screens show that person's name. Staff users live in the Identity module, and other modules may only use its `.Contracts` project ([0001](0001-modular-monolith.md)). Copying the display name into each module would go stale when a user is renamed.

## Decision

`IStaffDirectory` in `HireLab.Modules.Identity.Contracts` resolves user ids to their current display names, for a set of ids in one query (`FindDisplayNamesAsync`). The Identity module registers it as a scoped service.

It returns names for any staff user, whatever their team or active state: a user who moved to another team or was deactivated still shows as the author of what they did. Ids it doesn't know are missing from the result, and callers show a neutral fallback.

Modules store only the `UserId` and read the name again whenever they show it.

## Consequences

- A screen that shows authors costs one extra query per list, not one per row.
- Staff display names are visible to any signed-in staff user who can see something that user did. Staff names are not candidate personal data ([0023](0023-candidate-personal-data-stays-in-positions.md)).
- `HireLab.Modules.Identity.Contracts` references `HireLab.SharedKernel` for `UserId`.

## Status

Accepted
