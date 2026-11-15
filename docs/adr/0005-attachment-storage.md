# 0005. Attachment storage

## Context

Candidates have attachments, such as a CV. They are personal data: only staff on the candidate's team may read them, and closing a job position must really delete them (anonymisation). Files are small and uploaded only by authenticated staff.

## Decision

**Storage.** Azure Blob Storage, one private container `attachments`. Locally, Azurite through the Aspire AppHost.
- The storage account disables public network access to blobs, anonymous access and shared-key auth. The API reaches it with its managed identity.
- Blob soft delete and versioning are **off** for the container, so a deleted blob is gone.

**Blob layout.** `attachments/{candidateId}/{attachmentId}`. The original file name, content type and size are metadata in the `positions` schema; the blob name never contains user input.

**Uploads and downloads go through the API.** The API checks authorisation, then streams to or from the container. There are no SAS links.

**Limits.**
- At most 10 MB per file and 10 files per candidate.
- Allowed types: PDF, DOCX, TXT, MD, PNG, JPG.
- The type is checked by magic bytes, not just by extension or the declared content type.

**Deleting on anonymisation.** In one transaction, the attachment rows are deleted. After commit, the blobs are deleted. A periodic hosted service sweeps orphaned blobs (no matching row) as a safety net for deletions that failed.

**No malware scanning.** Only authenticated staff upload and download, and the type allow-list with magic-byte checks removes most of the risk. Defender for Storage malware scanning can be added later.

## Consequences

- One place enforces who can read a file. A leaked URL is useless without a staff session.
- All file traffic passes through the API. That's fine at CV sizes; large files would need a different approach.
- After a failed blob deletion, a file can outlive its row until the next sweep. The sweep interval bounds how long.
- With soft delete off, an accidental deletion can't be undone. That is the point for anonymisation, and attachments are not the system of record for anything else.
- An infected file uploaded by a staff member would be served to other staff members as-is.

## Status

Accepted
