# 0008. Candidate access

## Context

Candidates have no account. The interviewer sends them a candidate link by hand, and that link must give access to exactly one interview session, and only while it is in progress. The candidate side uses its own route group and hub ([0002](0002-backend-internal-style.md), [0006](0006-real-time-channel.md)). Staff use a cookie ([0007](0007-staff-authentication.md)).

## Decision

**Link and token.**
- A candidate link is `/s/{token}`, where the token is 256 bits of randomness.
- Only the token's SHA-256 hash is stored.

**Exchange for a cookie.**
- Opening the link calls an exchange endpoint that issues a separate candidate cookie: its own scheme and name, HttpOnly, Secure, `SameSite=Strict`, scoped to the candidate API and hub.
- The SPA then removes the token from the address bar.
- The candidate cookie carries the claims `sessionId` and `candidateLinkId`.

**Behaviour by session state:**

| State | Link opens | Candidate cookie |
|---|---|---|
| `Planned` | A waiting page that switches on its own when the session goes Live | Issued; only the waiting endpoints accept it |
| `Live` | The current task | Full candidate access |
| `Finished` | "This interview has ended" | Rejected |

The state is checked on every request, not only at exchange, so finishing a session cuts access immediately. The cookie also has a hard cap of 12 hours.

**Scoping.** Candidate endpoints have no session or candidate id in their route. The session comes only from the cookie's claims, so a candidate can't address anything but their own session.

**No device binding.** Several tabs or a device switch work. The interviewer can **regenerate** the link: that revokes the old token, invalidates cookies issued from it, and drops their hub connections. Presence shows how many candidate connections are open, so a second viewer is visible.

**CSRF and rate limiting** are the same as for staff: the antiforgery header on unsafe methods, and the rate limiter on the exchange endpoint.

## Consequences

- One mechanism (cookies) for both sides. Nothing sensitive stays in JavaScript, and no token stays in a URL after the first load.
- The interviewer can send the link before the call. An early candidate sees a waiting page, not an error.
- A forwarded link works for whoever opens it while the session is Live. Presence makes that visible and regeneration stops it, but nothing prevents it up front.
- Checking the session state on every candidate request costs one lookup per request. That's cheap, and it can be cached briefly if needed.

## Status

Accepted
