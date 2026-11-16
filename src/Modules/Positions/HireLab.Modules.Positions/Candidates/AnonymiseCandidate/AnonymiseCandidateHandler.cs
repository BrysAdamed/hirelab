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

namespace HireLab.Modules.Positions.Candidates.AnonymiseCandidate;

internal sealed record AnonymiseCandidateCommand(CandidateId CandidateId, uint Version);

/// <summary>
/// Anonymises one candidate of an open position for good (ADR 0023, ADR 0026). Refused while the candidate has an
/// unfinished interview session. In one transaction that first locks the candidate row (and share-locks the position,
/// so a concurrent close waits), the contact details are cleared and the attachment rows deleted. After commit the
/// blobs are deleted, best effort.
/// </summary>
internal sealed class AnonymiseCandidateHandler(
    PositionsDbContext dbContext,
    IUnfinishedSessionsQuery unfinishedSessions,
    AttachmentBlobCleanup blobCleanup,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<AnonymiseCandidateCommand, Result>
{
    public async Task<Result> HandleAsync(AnonymiseCandidateCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            SELECT 1 FROM positions.candidates c
            JOIN positions.job_positions p ON p.id = c.job_position_id
            WHERE c.id = {command.CandidateId.Value}
            FOR NO KEY UPDATE OF c FOR SHARE OF p
            """,
            cancellationToken);

        var candidate = await dbContext.Candidates.SingleOrDefaultAsync(candidate => candidate.Id == command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return CandidateErrors.NotFound;
        }

        dbContext.ExpectVersion(candidate, command.Version);
        var position = await dbContext.JobPositions.AsNoTracking()
            .SingleAsync(position => position.Id == candidate.JobPositionId, cancellationToken);
        var anonymised = candidate.Anonymise(position, currentUser.Id, timeProvider.GetUtcNow());
        if (anonymised.IsFailure)
        {
            return anonymised;
        }

        if (await unfinishedSessions.AnyForCandidatesAsync([candidate.Id], cancellationToken))
        {
            return CandidateErrors.UnfinishedSessions;
        }

        var attachments = await dbContext.Attachments
            .Where(attachment => attachment.CandidateId == candidate.Id)
            .ToListAsync(cancellationToken);
        dbContext.Attachments.RemoveRange(attachments);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await blobCleanup.DeleteAsync(attachments);
        return Result.Success();
    }
}
