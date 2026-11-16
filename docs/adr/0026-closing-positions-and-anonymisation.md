# 0026. Closing positions and anonymisation

## Context

A manager closes a job position when hiring ends, and may anonymise a single candidate who withdraws (HLB-43). Both are irreversible and remove personal data ([0023](0023-candidate-personal-data-stays-in-positions.md)): contact details and attachments. Attachments have a row in the `positions` schema and a blob in storage, and uploads may be in flight while the position is closed ([0024](0024-attachment-aggregate-and-upload-consistency.md)). Neither operation may happen while one of the candidates is still being interviewed, but interview sessions belong to the Sessions module, which doesn't exist yet (HLB-6).

## Decision

**The domain owns both operations.** `JobPosition.Close(candidates, by, at)` sets the state to `Closed` with `ClosedBy`/`ClosedAt` and anonymises every candidate not yet anonymised. `Candidate.Anonymise(position, by, at)` does it for one candidate of an open position. An anonymised candidate keeps who anonymised them and when; closing doesn't overwrite it.

**Sessions answers through a contract.** `IUnfinishedSessionsQuery` in `HireLab.Modules.Sessions.Contracts` tells whether any of a set of candidates has a session that isn't `Finished`. Positions references `Sessions.Contracts` and refuses with `409 JobPosition.UnfinishedSessions` or `409 Candidate.UnfinishedSessions`. Until HLB-6 the Sessions module registers an implementation that answers "none"; integration tests mark candidates through `HireLabTestHost.UnfinishedSessions`, which decorates the real implementation. `Sessions.Contracts` references `Positions.Contracts` for `CandidateId`.

**One transaction, locked first.** Closing starts a transaction with `SELECT … FOR NO KEY UPDATE` on the position row; anonymising takes `FOR NO KEY UPDATE` on the candidate row and `FOR SHARE` on its position. Both conflict with the `FOR SHARE` locks an upload holds, so an in-flight upload commits first, and only then are the candidates and their attachment rows read, which includes the new row; a later upload waits and is then refused. The same transaction updates the position or candidate rows and deletes the attachment rows. `NO KEY UPDATE` keeps foreign-key checks (adding a candidate, inserting an attachment) from blocking on the lock.

**Stale-click protection.** Both endpoints take the `version` the client read; a position or candidate changed since is refused with `Concurrency.ChangedMeanwhile`. Adding a candidate changes the position's version, so a close never anonymises fewer candidates than the manager saw without saying so.

**Blobs after commit.** Once the transaction commits, each deleted attachment's blob is deleted best effort through `AttachmentBlobCleanup`, which logs failures by id only ([0012](0012-observability.md)). A blob that survives is an orphan for the sweep (HLB-44).

**Manager only.** `POST /api/staff/positions/{id}/close` and `POST /api/staff/candidates/{id}/anonymise` are on the manager policy ([0009](0009-authorisation-model.md)); interviewers and the admin get `403`, another team's position or candidate is `404`.

## Consequences

- After commit no row points at personal data; the data that remains until the sweep runs is blobs without rows, which nothing serves.
- Closing a large position holds the position lock while it reads and updates its candidates; uploads to that position wait for it.
- HLB-6 replaces the stub with a query over sessions; the contract and the refusal codes stay.
- The unfinished-session check runs in the Positions transaction but can't lock sessions in another schema: a session started at the same moment as a close isn't seen. HLB-6 must refuse starting a session for an anonymised candidate.

## Status

Accepted
