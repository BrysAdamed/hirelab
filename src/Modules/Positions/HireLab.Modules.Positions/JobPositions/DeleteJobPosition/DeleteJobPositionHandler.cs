using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.DeleteJobPosition;

internal sealed record DeleteJobPositionCommand(JobPositionId PositionId);

/// <summary>Deletes a job position for good. Only an open position can be deleted.</summary>
internal sealed class DeleteJobPositionHandler(PositionsDbContext dbContext) : ICommandHandler<DeleteJobPositionCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteJobPositionCommand command, CancellationToken cancellationToken)
    {
        var position = await dbContext.JobPositions.SingleOrDefaultAsync(position => position.Id == command.PositionId, cancellationToken);
        if (position is null)
        {
            return JobPositionErrors.NotFound;
        }

        // TODO HLB-41: refuse with JobPosition.HasCandidates while the position has candidates.
        var deletable = position.EnsureCanBeDeleted();
        if (deletable.IsFailure)
        {
            return deletable;
        }

        dbContext.JobPositions.Remove(position);
        return Result.Success();
    }
}
