using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// A file on a candidate's profile, such as the CV (ADR 0005). The row holds the metadata; the content is the blob
/// <see cref="BlobName"/> in the <c>attachments</c> container. Its own team-owned aggregate next to <see cref="Candidate"/>,
/// so uploads don't change the candidate's version. Each attachment takes one of the candidate's
/// <see cref="MaxPerCandidate"/> slots, and a unique index on the slot keeps the limit under concurrent uploads.
/// </summary>
internal sealed class Attachment : ITeamOwned
{
    public const long MaxSizeInBytes = 10 * 1024 * 1024;
    public const int MaxPerCandidate = 10;
    public const int FileNameMaxLength = 255;

    private Attachment()
    {
    }

    public AttachmentId Id { get; private set; }

    /// <summary>The candidate's team, copied on upload so the team query filter covers attachments directly (ADR 0009).</summary>
    public TeamId TeamId { get; private set; }

    public CandidateId CandidateId { get; private set; }

    /// <summary>The slot (1 to <see cref="MaxPerCandidate"/>) the attachment takes among the candidate's attachments.</summary>
    public int Slot { get; private set; }

    /// <summary>The name the file was uploaded with. Personal data: never logged (ADR 0012).</summary>
    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long Size { get; private set; }

    public UserId UploadedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the attachment is saved: the upload time.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public string BlobName => BlobNameFor(CandidateId, Id);

    /// <summary>The blob name of an attachment: <c>{candidateId}/{attachmentId}</c>, never user input.</summary>
    public static string BlobNameFor(CandidateId candidateId, AttachmentId attachmentId) => $"{candidateId}/{attachmentId}";

    /// <summary>
    /// Records an uploaded file as the candidate's attachment in their lowest free slot. Only while the candidate's
    /// position is open and the candidate isn't anonymised, and only when one of their slots is free.
    /// </summary>
    public static Result<Attachment> Upload(
        AttachmentId id,
        Candidate candidate,
        JobPosition position,
        IReadOnlyCollection<int> takenSlots,
        UploadedFile file,
        UserId uploadedBy)
    {
        ArgumentNullException.ThrowIfNull(file);

        var canUpload = EnsureCanUpload(candidate, position, takenSlots);
        if (canUpload.IsFailure)
        {
            return canUpload.Error;
        }

        return new Attachment
        {
            Id = id,
            TeamId = candidate.TeamId,
            CandidateId = candidate.Id,
            Slot = FreeSlot(takenSlots)!.Value,
            FileName = file.FileName,
            ContentType = file.Type.ContentType,
            Size = file.Size,
            UploadedBy = uploadedBy,
        };
    }

    /// <summary>Whether a file may be uploaded to the candidate whose attachments take <paramref name="takenSlots"/>.</summary>
    public static Result EnsureCanUpload(Candidate candidate, JobPosition position, IReadOnlyCollection<int> takenSlots)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(takenSlots);

        var canChange = candidate.EnsureAttachmentsCanChange(position);
        if (canChange.IsFailure)
        {
            return canChange;
        }

        return FreeSlot(takenSlots) is null ? AttachmentErrors.LimitReached : Result.Success();
    }

    private static int? FreeSlot(IReadOnlyCollection<int> takenSlots)
    {
        for (var slot = 1; slot <= MaxPerCandidate; slot++)
        {
            if (!takenSlots.Contains(slot))
            {
                return slot;
            }
        }

        return null;
    }
}

/// <summary>An uploaded file whose type has been detected from its content.</summary>
internal sealed record UploadedFile(string FileName, AttachmentType Type, long Size);
