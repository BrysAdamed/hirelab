# 0007. Staff authentication

## Context

Staff (admin, manager, interviewer) log in with local accounts; SSO with Entra ID must be addable later without rework. The only clients are our two browser SPAs, both using SignalR ([0006](0006-real-time-channel.md)). There is no email delivery: invite and reset links are copied and sent by hand. The main threat to a browser session is XSS.

We compared an HttpOnly cookie with bearer tokens held by the SPA. Bearer tokens suit non-browser clients and cross-origin SPAs, neither of which we have. They expose the token to injected scripts and force tokens into WebSocket query strings.

## Decision

**ASP.NET Core Identity with a cookie.**
- The cookie is HttpOnly, Secure, `SameSite=Strict`.
- Login, logout and account endpoints are our own, in the Identity module. We don't use `MapIdentityApi`.
- Both SPAs must be served same-site with the API (HLB-16 honours this).
- SignalR authenticates with the same cookie.

**CSRF.**
- `SameSite=Strict` on the cookie.
- The ASP.NET Core antiforgery token is required on every unsafe method. The API sends it as a readable `XSRF-TOKEN` cookie, and the SPA's fetch wrapper copies it into an `X-XSRF-TOKEN` header.
- The API accepts only `application/json` request bodies, except for file uploads.

**Session lifetime.**
- Sliding expiration after 8 hours of inactivity, with an absolute maximum of 7 days. No "remember me".
- The security stamp is validated every 5 minutes, so deactivation, a role change or a password reset ends existing sessions within that window.

**HireLab owns the user.** The `UserId`, role and team live in our tables. An identity provider is only a way to log in: Entra ID will be added as an OIDC external login (Identity's `UserLogins`) linked to an existing user. IdP claims never create users or grant roles.

**Passwords.**
- At least 12 characters, with no composition rules.
- A 15-minute lockout after 5 failed attempts.
- No breached-password check, which would need an external service.
- No MFA for now. Identity's TOTP support leaves room for it.

**Invite links.**
- One-time, valid for 72 hours, stored as a hash. Each one creates an account with a fixed role and team, and the invitee chooses their password.
- Admins invite managers. Managers invite interviewers into their own team.
- An unused invite can be revoked.

**Reset links.** There is no self-service "forgot password". Resets work like invites:
- An admin generates a one-time reset link (valid 24 hours) for a manager; a manager generates one for their interviewers.
- The link is sent by hand.

**First admin.**
- Azure: if no admin exists, the migration job ([0004](0004-database-and-ef-core.md)) creates a one-time admin invite valid for 24 hours and writes the link to its job log. The same job, triggered manually, issues a new link if the admin is locked out.
- Locally: the development seed creates the admin with a password from the seed file.

**Rate limiting.** The built-in rate limiter, per IP with a fixed window of about 10 requests a minute, covers login, candidate link exchange, and invite and reset link redemption.

## Consequences

- No credential is ever readable by JavaScript, so XSS can misuse a session while the page is open but can't steal it.
- The SPAs can't move to another domain without revisiting this decision.
- Adding Entra ID later means adding an external login provider and a linking flow; roles, teams and the SPAs don't change.
- No email means every invite, reset and bootstrap link passes through a person, or through a log in the bootstrap case. The links are one-time and short-lived to limit that exposure.
- The bootstrap link appears in job logs, so anyone with log access during those 24 hours could claim the admin account.
- A non-browser client would need a second scheme added later.

## Status

Accepted
