using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Attachments.Storage;

/// <summary>
/// The content of attachments: one blob per attachment, named <c>{candidateId}/{attachmentId}</c> (ADR 0005). The
/// metadata rows are the source of truth; a blob without a row is an orphan that the sweep removes.
/// </summary>
internal interface IAttachmentStore
{
    /// <summary>Stores the content of a new attachment. Fails when the blob already exists.</summary>
    Task UploadAsync(CandidateId candidateId, AttachmentId attachmentId, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Opens the content of an attachment for reading, or returns <c>null</c> when its blob doesn't exist.</summary>
    Task<Stream?> OpenReadAsync(CandidateId candidateId, AttachmentId attachmentId, CancellationToken cancellationToken);

    /// <summary>Deletes the content of an attachment. Deleting a blob that doesn't exist succeeds.</summary>
    Task DeleteAsync(CandidateId candidateId, AttachmentId attachmentId, CancellationToken cancellationToken);

    /// <summary>Every stored blob with its last-modified time. Blobs whose name isn't an attachment blob name are skipped.</summary>
    IAsyncEnumerable<StoredAttachment> ListAsync(CancellationToken cancellationToken);
}

/// <summary>A stored attachment blob.</summary>
internal sealed record StoredAttachment(CandidateId CandidateId, AttachmentId AttachmentId, DateTimeOffset LastModified);
