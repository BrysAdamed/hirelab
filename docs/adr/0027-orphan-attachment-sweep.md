# 0027. Orphan attachment sweep

## Context

[0005](0005-attachment-storage.md) and [0024](0024-attachment-aggregate-and-upload-consistency.md) write an attachment's blob before its row and delete the row before the blob, both best effort. A crash or a failed blob call leaves an orphan: a blob in `attachments` with no attachment row. Orphans can hold personal data that anonymisation was meant to remove, so a periodic sweep deletes them. Open questions: where it runs, how it avoids deleting an upload whose row isn't committed yet, how it scales with the container, what it does with blobs it doesn't recognise, and how it bypasses the team query filter ([0009](0009-authorisation-model.md)) without weakening the rule.

## Decision

**A hosted service in the API.** `OrphanAttachmentSweep` (a `BackgroundService` in the Positions module) runs a sweep one minute after the API starts and then every `Interval`, on a `PeriodicTimer` driven by the injected `TimeProvider`. The work itself is `OrphanAttachmentSweeper.SweepAsync`, resolved in a fresh scope for every sweep, so tests run it directly. The first sweep waits a minute so it doesn't compete with startup and never runs in a host that starts only briefly, such as the build-time OpenAPI generation.

**What is an orphan.** A blob whose name parses as `{candidateId}/{attachmentId}` (exactly the name `Attachment.BlobNameFor` builds), that is older than `GracePeriod` by its last-modified time, and for which no attachment row has that candidate and attachment id. The grace period keeps an upload in flight, whose blob exists but whose row isn't committed yet.

**Batches.** The blob listing is read a page at a time (`PageSize` blobs). For each page, the blobs older than the grace period are checked against `positions.attachments` with one query, and the orphans among them are deleted before the next page is read. Memory stays bounded by the page size, whatever the size of the container.

**Unrecognised blob names are kept.** Only `IAttachmentStore` writes to the container, always with an attachment blob name. A blob with any other name didn't come from an upload, so the sweep can't tell what it is, and with soft delete off ([0005](0005-attachment-storage.md)) a wrong deletion can't be undone. The sweep counts such blobs and logs a warning with the count only, never the names, which could contain anything.

**Failures.** A blob that fails to delete is logged with its ids and counted; the sweep goes on with the rest and the next sweep retries it. A failure of the whole sweep (listing, database) is logged and the next sweep runs on schedule. Cancellation on shutdown ends the loop quietly.

**Bypassing the team filter.** The sweeper reads rows of every team with `IgnoreQueryFilters`. It is marked with `ISystemJob` (SharedKernel), and the architecture test allows the bypass in the migration worker, in hosted services and in types marked `ISystemJob`. The marker makes a system job explicit in code and review; it is not for request handlers.

**Settings.** Bound from `OrphanAttachmentSweep` and validated on start: `Enabled` (default `true`), `Interval` (default 1 hour, at least 1 minute), `GracePeriod` (default 1 hour, at least 5 minutes) and `PageSize` (default 500, at most 5000, the Blob Storage maximum). The integration test host sets `Enabled` to `false`, so no sweep deletes blobs behind a test's back.

**Telemetry.** Logs only, with counts and ids ([0012](0012-observability.md)): one line per deleted orphan, one per failed deletion and a summary per sweep. No metric: with no alerts and one user, the summary line is enough.

## Consequences

- An orphan lives at most `GracePeriod` plus `Interval` while the API runs.
- The API scales to zero ([0010](0010-hosting-and-environments.md)), so no sweep runs while it is idle, and a replica that lives less than a minute never sweeps. Orphans then wait for the next active period. That is acceptable: they are rare, and nobody can read them in the meantime because downloads go through rows.
- Each sweep lists the whole container. At CV volumes that is a few pages; a much larger container would want a cheaper trigger, such as a queue of failed deletions.
- A blob with an unrecognised name stays until someone removes it by hand; the warning says it exists.
- New system jobs that need rows of every team implement `ISystemJob`, or are hosted services.

## Status

Accepted
