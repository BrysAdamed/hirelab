using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.ListJobPositions;

internal sealed record ListJobPositionsQuery(JobPositionState State);

internal sealed record JobPositionSummaryResponse(Guid Id, string Title, JobPositionState State, DateTimeOffset CreatedAt);

internal sealed class ListJobPositionsHandler(PositionsDbContext dbContext)
    : IQueryHandler<ListJobPositionsQuery, Result<IReadOnlyList<JobPositionSummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<JobPositionSummaryResponse>>> HandleAsync(
        ListJobPositionsQuery query,
        CancellationToken cancellationToken) =>
        await dbContext.JobPositions.AsNoTracking()
            .Where(position => position.State == query.State)
            .OrderByDescending(position => position.CreatedAt)
            .ThenByDescending(position => position.Id)
            .Select(position => new JobPositionSummaryResponse(position.Id.Value, position.Title, position.State, position.CreatedAt))
            .ToListAsync(cancellationToken);
}
