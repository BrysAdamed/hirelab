using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.Positions.Attachments.DeleteAttachment;

internal sealed record DeleteAttachmentCommand(CandidateId CandidateId, AttachmentId AttachmentId);

/// <summary>
/// Deletes an attachment while the candidate's position is open (ADR 0005): the row is deleted and committed first,
/// then the blob, best effort. A blob that survives is an orphan for the sweep.
/// </summary>
internal sealed partial class DeleteAttachmentHandler(
    PositionsDbContext dbContext,
    IAttachmentStore store,
    ILogger<DeleteAttachmentHandler> logger)
    : ICommandHandler<DeleteAttachmentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteAttachmentCommand command, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.Attachments.SingleOrDefaultAsync(
            attachment => attachment.Id == command.AttachmentId && attachment.CandidateId == command.CandidateId,
            cancellationToken);
        if (attachment is null)
        {
            return AttachmentErrors.NotFound;
        }

        var candidate = await dbContext.Candidates.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == attachment.CandidateId, cancellationToken);
        var position = await dbContext.JobPositions.AsNoTracking()
            .SingleAsync(position => position.Id == candidate.JobPositionId, cancellationToken);
        var canChange = candidate.EnsureAttachmentsCanChange(position);
        if (canChange.IsFailure)
        {
            return canChange;
        }

        dbContext.Attachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await store.DeleteAsync(attachment.CandidateId, attachment.Id, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogBlobNotDeleted(logger, exception, attachment.CandidateId.Value, attachment.Id.Value);
        }

        return Result.Success();
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The blob of deleted attachment {AttachmentId} of candidate {CandidateId} was not deleted; the sweep will remove it")]
    private static partial void LogBlobNotDeleted(ILogger logger, Exception exception, Guid candidateId, Guid attachmentId);
}
