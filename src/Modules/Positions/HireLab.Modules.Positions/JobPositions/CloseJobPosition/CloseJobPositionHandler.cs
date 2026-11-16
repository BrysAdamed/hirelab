using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.CloseJobPosition;

internal sealed record CloseJobPositionCommand(JobPositionId PositionId, uint Version);

/// <summary>
/// Closes a job position for good and anonymises its candidates (ADR 0023, ADR 0026). Refused while any of its
/// candidates has an unfinished interview session. In one transaction that first locks the position row, so in-flight
/// uploads (which share-lock it) finish before and later ones are refused, the position is closed, the candidates'
/// contact details cleared and their attachment rows deleted. After commit the blobs are deleted, best effort.
/// </summary>
internal sealed class CloseJobPositionHandler(
    PositionsDbContext dbContext,
    IUnfinishedSessionsQuery unfinishedSessions,
    AttachmentBlobCleanup blobCleanup,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CloseJobPositionCommand, Result>
{
    public async Task<Result> HandleAsync(CloseJobPositionCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM positions.job_positions WHERE id = {command.PositionId.Value} FOR NO KEY UPDATE",
            cancellationToken);

        var position = await dbContext.JobPositions.SingleOrDefaultAsync(position => position.Id == command.PositionId, cancellationToken);
        if (position is null)
        {
            return JobPositionErrors.NotFound;
        }

        dbContext.ExpectVersion(position, command.Version);
        var candidates = await dbContext.Candidates
            .Where(candidate => candidate.JobPositionId == position.Id)
            .ToListAsync(cancellationToken);
        var closed = position.Close(candidates, currentUser.Id, timeProvider.GetUtcNow());
        if (closed.IsFailure)
        {
            return closed;
        }

        var candidateIds = candidates.Select(candidate => candidate.Id).ToList();
        if (await unfinishedSessions.AnyForCandidatesAsync(candidateIds, cancellationToken))
        {
            return JobPositionErrors.UnfinishedSessions;
        }

        var attachments = await dbContext.Attachments
            .Where(attachment => candidateIds.Contains(attachment.CandidateId))
            .ToListAsync(cancellationToken);
        dbContext.Attachments.RemoveRange(attachments);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await blobCleanup.DeleteAsync(attachments);
        return Result.Success();
    }
}
