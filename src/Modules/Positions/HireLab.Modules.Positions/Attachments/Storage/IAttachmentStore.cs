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

    /// <summary>
    /// Every stored blob with its last-modified time, a page of at most <paramref name="pageSize"/> blobs at a time.
    /// Blobs whose name isn't an attachment blob name are only counted.
    /// </summary>
    IAsyncEnumerable<StoredAttachmentPage> ListPagesAsync(int pageSize, CancellationToken cancellationToken);
}

/// <summary>A stored attachment blob.</summary>
internal sealed record StoredAttachment(CandidateId CandidateId, AttachmentId AttachmentId, DateTimeOffset LastModified);

/// <summary>One page of the blob listing: the attachment blobs, and how many blobs had a name that isn't an attachment blob name.</summary>
internal sealed record StoredAttachmentPage(IReadOnlyList<StoredAttachment> Attachments, int UnrecognisedBlobs);
