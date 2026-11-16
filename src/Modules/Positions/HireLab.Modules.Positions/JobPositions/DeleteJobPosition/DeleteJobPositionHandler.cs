using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.DeleteJobPosition;

internal sealed record DeleteJobPositionCommand(JobPositionId PositionId);

/// <summary>
/// Deletes a job position for good. Only an open position without candidates can be deleted; a candidate added
/// meanwhile changes the position's version, so the delete then fails with <c>Concurrency.ChangedMeanwhile</c>.
/// </summary>
internal sealed class DeleteJobPositionHandler(PositionsDbContext dbContext) : ICommandHandler<DeleteJobPositionCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteJobPositionCommand command, CancellationToken cancellationToken)
    {
        var position = await dbContext.JobPositions.SingleOrDefaultAsync(position => position.Id == command.PositionId, cancellationToken);
        if (position is null)
        {
            return JobPositionErrors.NotFound;
        }

        var hasCandidates = await dbContext.Candidates.AnyAsync(candidate => candidate.JobPositionId == position.Id, cancellationToken);
        var deletable = position.EnsureCanBeDeleted(hasCandidates);
        if (deletable.IsFailure)
        {
            return deletable;
        }

        dbContext.JobPositions.Remove(position);
        return Result.Success();
    }
}
