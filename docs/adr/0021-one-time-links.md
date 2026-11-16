# 0021. One-time links

## Context

[0007](0007-staff-authentication.md) gives staff accounts through links sent by hand: invite links (72 hours), reset links (24 hours) and the first admin's bootstrap invite (24 hours, written to the migration job's log). They all carry a secret that works once and then expires. Without a shared design, each would pick its own token format, storage and states, and a link could leak through a server log, a `Referer` header or a database dump.

## Decision

**One token format and one base type.**
- A token is 256 random bits, base64url encoded (`OneTimeToken`). Only its SHA-256 hash is stored, under a unique index. A database dump doesn't give working links.
- `OneTimeLink` is the base class of every link entity in the Identity module: token hash, expiry, used-at, revoked-at and an `xmin` concurrency token. `Invite` and `ResetLink` derive from it, each in its own table.
- The state is derived from those columns and the clock: `Pending`, `Used`, `Revoked` or `Expired` (`OneTimeLinkState`). Only a pending link can be used. A link that is neither used nor revoked is *open*; revoking works on open links, including expired ones.
- Errors use the link's prefix (`Invite.Expired`, `Invite.Revoked`, `Invite.AlreadyUsed`), so the page can say what happened.

**The token never travels in a URL the server sees.** The link is a staff app page with the token in the fragment (`/invite#<token>`). The page posts the token in the request body to anonymous, rate-limited endpoints ([0007](0007-staff-authentication.md)): one to preview the link, one to redeem it. The API returns the link's path once, when it is issued, and the UI builds the absolute URL from its own origin.

**Invites.**
- An invite stores the invitee's email (and its normalised form), display name, role, an existing `TeamId` or the name of a new team, and who issued it (empty for the bootstrap invite).
- Each email has at most one open invite: issuing a new one revokes the open ones in the same transaction, and a unique partial index on the normalised email enforces it.
- The email must not belong to a user, both when the invite is issued and when it is redeemed.
- Redemption creates the team (for a new-team invite), marks the invite used and creates the user in one save. The concurrency token stops a second concurrent redemption; the user indexes of [0020](0020-teams-and-user-lifecycle.md) stop a second active manager or admin. The new user is signed in at once.
- Invites are not `ITeamOwned`: a manager invite for a new team has no team yet, and the admin manages all manager invites. The handlers scope them instead: the admin sees manager invites, a manager sees the interviewer invites of their own team, and anything else is not found.

**Reset links** (HLB-38).
- `ResetLink : OneTimeLink`, valid for 24 hours, in its own table. It stores the user it resets and who issued it (empty when the system issues it, for the admin recovery of HLB-39). The link is `/reset#<token>`.
- `ResetLinkIssuer` issues a link for an active user and revokes that user's open link in the same transaction; a unique partial index allows one open link per user. Who may issue for whom is checked by the endpoint: the admin for any manager, a manager for the interviewers of their own team. Anyone else is not found.
- Redemption refuses a deactivated user. It sets the new password, clears the lockout, changes the security stamp (ending existing sessions within the validation window) and marks the link used, all in one save. The concurrency token stops a second concurrent redemption. Unlike an invite, it doesn't sign the user in: they log in with the new password, which also proves they know it.
- Reset links aren't listed: a lost link is replaced by generating a new one.

## Consequences

- A lost link can't be shown again; the inviter issues a new one, which revokes the old.
- Expired invites stay in the table. Lists show them for 30 days after they expire.
- Reset links reuse the token, hashing, expiry, state and error handling, and add only their own entity and endpoints.
- Setting a password without the current one or an Identity token needs `StaffUserManager.ReplacePasswordAsync`; the redeemed reset link is the proof. Identity's token providers aren't registered.
- The bootstrap job (HLB-39) issues an admin invite through the same `InviteIssuer` and needs a configured public base URL to print the full link.

## Status

Accepted
