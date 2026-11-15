# 0006. Real-time channel

## Context

During a live interview the interviewer watches the candidate's responses arrive, and the candidate's screen must follow the task queue the interviewer controls. Both sides need server push. The app runs as a single modular monolith ([0001](0001-modular-monolith.md)) on Container Apps with scale-to-zero, and writes already go through HTTP handlers ([0002](0002-backend-internal-style.md)).

## Decision

**Transport.** ASP.NET Core SignalR, with JSON as the protocol. WebSockets are preferred; the SSE and long-polling fallbacks stay enabled for candidates behind proxies that block WebSockets.

**Two hubs**, mirroring the API split:

| Hub | Clients | Groups |
|---|---|---|
| `/hubs/staff` | Interviewer and manager views | One per interview session they open |
| `/hubs/candidate` | The candidate app | The candidate's own session only |

Each hub requires the same auth policy as its route group (HLB-15 decides the schemes).

**Push-only.** Every write goes through the HTTP endpoints. The only client → server hub calls are subscribing and unsubscribing, and the server checks access before adding a connection to a group.

**Thin events.** Messages carry ids and a revision, never content, for example `ResponseUpdated { sessionTaskId, revision }`. The client refetches over HTTP. HTTP is the single source of truth, and hub traffic carries no personal data.

**Events in scope:**

| To staff | To the candidate |
|---|---|
| Response changed | Current task or queue changed |
| Task queue changed | Session state changed (`Finished` locks the UI) |
| Session state changed | |
| Candidate connected or disconnected | |

New events are added when a feature needs them.

**Candidate presence.** The staff hub reports whether the candidate is connected now and when they were last seen. It is tracked in memory and not persisted.

**Where pushing happens.** Modules raise integration events after commit, as usual. A `Realtime` component in the API host subscribes to them and sends the hub messages. Modules never reference SignalR.

**Typed contracts.** Hubs are strongly typed (`Hub<IStaffClient>`, `Hub<ICandidateClient>`). TypeScript types are generated from them (for example `TypedSignalR.Client.TypeScript`), committed, and checked for drift in CI like the API client. If the generator turns out to be a poor fit, we write the types by hand and add a contract test.

**Scale-out.** One API replica (max replicas = 1), with no backplane. An open connection keeps the replica alive, so it doesn't scale to zero mid-interview.

**Reconnect.** Clients reconnect automatically with backoff and show a "reconnecting…" state. After every reconnect they refetch the full current state over HTTP. There is no message replay or sequence numbering.

## Consequences

- A lost or out-of-order message only delays an update; it can't corrupt client state.
- Every push costs an HTTP refetch. That's cheap at autosave cadence and a handful of viewers.
- A revision swap or restart drops every connection. Clients recover by reconnecting and refetching, and the candidate sees a short "reconnecting…" state.
- One replica caps throughput and means a crash is a short outage. Moving to Azure SignalR Service (Free tier) is a configuration change when that matters.
- Presence held in memory resets on restart and would need a shared store with more than one replica.

## Status

Accepted
