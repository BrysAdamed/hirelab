using HireLab.Modules.Positions.Domain;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.Positions.Attachments.Storage;

/// <summary>
/// Deletes the blobs of attachments whose rows were deleted and committed, best effort (ADR 0024): a blob that can't be
/// deleted is logged by id and left as an orphan for the sweep.
/// </summary>
internal sealed partial class AttachmentBlobCleanup(IAttachmentStore store, ILogger<AttachmentBlobCleanup> logger)
{
    public async Task DeleteAsync(IEnumerable<Attachment> deletedAttachments)
    {
        ArgumentNullException.ThrowIfNull(deletedAttachments);

        foreach (var attachment in deletedAttachments)
        {
            try
            {
                await store.DeleteAsync(attachment.CandidateId, attachment.Id, CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogBlobNotDeleted(logger, exception, attachment.CandidateId.Value, attachment.Id.Value);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The blob of deleted attachment {AttachmentId} of candidate {CandidateId} was not deleted; the sweep will remove it")]
    private static partial void LogBlobNotDeleted(ILogger logger, Exception exception, Guid candidateId, Guid attachmentId);
}
