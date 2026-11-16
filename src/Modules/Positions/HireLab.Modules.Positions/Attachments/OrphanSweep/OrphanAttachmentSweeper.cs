using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Positions.Attachments.OrphanSweep;

/// <summary>
/// Deletes attachment blobs that have no attachment row and are older than the grace period (ADR 0005, ADR 0027). Blobs
/// whose name isn't an attachment blob name are kept and only counted. A failed deletion is logged and the sweep goes on.
/// </summary>
internal sealed partial class OrphanAttachmentSweeper(
    PositionsDbContext dbContext,
    IAttachmentStore store,
    TimeProvider timeProvider,
    IOptions<OrphanAttachmentSweepOptions> options,
    ILogger<OrphanAttachmentSweeper> logger)
    : ISystemJob
{
    public async Task<OrphanAttachmentSweepResult> SweepAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var cutoff = timeProvider.GetUtcNow() - settings.GracePeriod;
        var result = new OrphanAttachmentSweepResult(0, 0, 0, 0);

        await foreach (var page in store.ListPagesAsync(settings.PageSize, cancellationToken))
        {
            var orphans = await OrphansAsync(page.Attachments.Where(blob => blob.LastModified < cutoff).ToList(), cancellationToken);
            var deleted = 0;
            foreach (var orphan in orphans)
            {
                deleted += await TryDeleteAsync(orphan, cancellationToken) ? 1 : 0;
            }

            result = result with
            {
                Checked = result.Checked + page.Attachments.Count,
                Deleted = result.Deleted + deleted,
                Failed = result.Failed + orphans.Count - deleted,
                Unrecognised = result.Unrecognised + page.UnrecognisedBlobs,
            };
        }

        if (result.Unrecognised > 0)
        {
            LogUnrecognisedBlobsKept(result.Unrecognised);
        }

        LogSwept(result.Checked, result.Deleted, result.Failed, result.Unrecognised);
        return result;
    }

    private async Task<List<StoredAttachment>> OrphansAsync(List<StoredAttachment> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var attachmentIds = candidates.Select(blob => blob.AttachmentId).ToList();
        var rows = await dbContext.Attachments
            .IgnoreQueryFilters()
            .Where(attachment => attachmentIds.Contains(attachment.Id))
            .Select(attachment => new { attachment.CandidateId, attachment.Id })
            .ToListAsync(cancellationToken);

        var withRow = rows.Select(row => (row.CandidateId, row.Id)).ToHashSet();
        return candidates.Where(blob => !withRow.Contains((blob.CandidateId, blob.AttachmentId))).ToList();
    }

    private async Task<bool> TryDeleteAsync(StoredAttachment orphan, CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteAsync(orphan.CandidateId, orphan.AttachmentId, cancellationToken);
            LogOrphanDeleted(orphan.CandidateId, orphan.AttachmentId);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogOrphanDeleteFailed(exception, orphan.CandidateId, orphan.AttachmentId);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted the orphaned blob of attachment {AttachmentId} of candidate {CandidateId}")]
    private partial void LogOrphanDeleted(CandidateId candidateId, AttachmentId attachmentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Deleting the orphaned blob of attachment {AttachmentId} of candidate {CandidateId} failed; the next sweep retries it")]
    private partial void LogOrphanDeleteFailed(Exception exception, CandidateId candidateId, AttachmentId attachmentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Kept {Count} blobs in the attachments container whose name isn't an attachment blob name")]
    private partial void LogUnrecognisedBlobsKept(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Orphan attachment sweep checked {Checked} blobs: {Deleted} orphans deleted, {Failed} failed, {Unrecognised} unrecognised")]
    private partial void LogSwept(int @checked, int deleted, int failed, int unrecognised);
}

/// <summary>What one sweep did: attachment blobs checked, orphans deleted and failed to delete, and unrecognised blobs kept.</summary>
internal sealed record OrphanAttachmentSweepResult(int Checked, int Deleted, int Failed, int Unrecognised);
