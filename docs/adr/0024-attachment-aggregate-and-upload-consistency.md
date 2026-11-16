# 0024. Attachment aggregate and upload consistency

## Context

[0005](0005-attachment-storage.md) decides where attachment content lives and what is allowed: one private `attachments` container, blob name `{candidateId}/{attachmentId}`, metadata in the `positions` schema, 10 MB per file, 10 files per candidate, a type allow-list checked by content, blob first and row second on upload. It leaves open how the metadata is modelled, how the limit holds when two uploads race, and how an upload stays consistent with closing a position or anonymising a candidate (HLB-43), which must leave no attachment behind.

## Decision

**Its own team-owned aggregate.** `Attachment` is a root next to `Candidate`, not an entity inside it. It carries `TeamId` (copied from the candidate) and the team query filter covers it directly ([0009](0009-authorisation-model.md)). Uploads and deletes don't touch the candidate row, so they don't change the candidate's version and don't turn a colleague's concurrent edit of the contact details into `Concurrency.ChangedMeanwhile`. Who may change attachments is still the candidate's rule (`Candidate.EnsureAttachmentsCanChange`: position open, candidate not anonymised).

**The limit is a slot.** Each attachment takes a `Slot` from 1 to 10 among its candidate's attachments: a unique index on `(candidate_id, slot)` and a check constraint on the range make more than 10 impossible in the database. An upload takes the lowest free slot; when a concurrent upload took it first, the insert hits the unique index and the upload retries with fresh slots, ending with `Attachment.LimitReached` once all ten are taken.

**Uploads lock the candidate.** The row is inserted in a transaction that first takes `FOR SHARE` locks on the candidate and position rows, then re-reads them through the team filter and applies the rules. Closing a position and anonymising a candidate update those rows, so they either finish before the upload reads them (and it is refused) or wait until it commits (and then delete its row too). Concurrent uploads share the lock and don't block each other.

**Order and clean-up.** Upload: checks, then the blob, then the row; when the row isn't saved the blob is deleted best effort. Delete: the row is deleted and committed, then the blob, best effort. A blob without a row is an orphan; the sweep (HLB-44) removes them.

**Blob access behind `IAttachmentStore`.** Upload (fails if the blob exists), open-read, delete-if-exists and list with the last-modified time. The module's handlers, anonymisation and the sweep use it; nothing else in the module touches the Blob SDK.

**Content checks are a pure class.** `AttachmentTypeDetector` decides the type from the extension and proves it with the content (magic bytes, a DOCX ZIP's `[Content_Types].xml` and `word/` part, or valid UTF-8 without NUL bytes). The stored and served content type is the detected type's, never the uploader's.

**Request size.** The upload endpoint carries a request size limit of 10 MB plus 64 KB for the multipart framing, so Kestrel refuses a larger body with `413` before reading it; a file just over 10 MB is refused by the handler with `Attachment.TooLarge`.

**Downloads.** Streamed from the blob with the stored content type, `Content-Disposition: attachment` with the original name (RFC 6266 `filename*` encoding), `X-Content-Type-Options: nosniff` and `Cache-Control: private, no-store`.

## Consequences

- The limit holds under any concurrency without serialising uploads.
- HLB-43 must update the candidate (or position) row in the same transaction as deleting the attachment rows, so an in-flight upload is serialised with it.
- The candidate's `Version` says nothing about its attachments; the attachments list is read separately.
- A crash between blob and row, or a failed blob delete, leaves an orphan until the sweep runs.
- Azurite rejects the newer storage API versions of the Azure SDK, so it runs with `--skipApiVersionCheck`, as Aspire runs it in the AppHost.

## Status

Accepted
