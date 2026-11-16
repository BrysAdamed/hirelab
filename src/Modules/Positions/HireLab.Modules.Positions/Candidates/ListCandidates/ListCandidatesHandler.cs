using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates.ListCandidates;

internal sealed record ListCandidatesQuery(JobPositionId PositionId);

internal sealed record PositionCandidateResponse(
    Guid Id,
    int Number,
    string DisplayName,
    string? Email,
    string? Phone,
    bool IsAnonymised,
    DateTimeOffset CreatedAt);

internal sealed class ListCandidatesHandler(PositionsDbContext dbContext)
    : IQueryHandler<ListCandidatesQuery, Result<IReadOnlyList<PositionCandidateResponse>>>
{
    public async Task<Result<IReadOnlyList<PositionCandidateResponse>>> HandleAsync(
        ListCandidatesQuery query,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.JobPositions.AnyAsync(position => position.Id == query.PositionId, cancellationToken))
        {
            return JobPositionErrors.NotFound;
        }

        var candidates = await dbContext.Candidates.AsNoTracking()
            .Where(candidate => candidate.JobPositionId == query.PositionId)
            .OrderBy(candidate => candidate.Number)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate => new PositionCandidateResponse(
                candidate.Id.Value,
                candidate.Number,
                candidate.DisplayName,
                candidate.Email,
                candidate.Phone,
                candidate.IsAnonymised,
                candidate.CreatedAt))
            .ToList();
    }
}
