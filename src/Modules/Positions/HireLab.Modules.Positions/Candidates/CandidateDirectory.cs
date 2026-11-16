using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates;

internal sealed class CandidateDirectory(PositionsDbContext dbContext) : ICandidateDirectory
{
    public async Task<CandidateSummary?> FindAsync(CandidateId candidateId, CancellationToken cancellationToken = default)
    {
        var found = await WithPositionState(dbContext.Candidates.Where(candidate => candidate.Id == candidateId))
            .SingleOrDefaultAsync(cancellationToken);
        return found is null ? null : Summarise(found.Candidate, found.State);
    }

    public async Task<IReadOnlyDictionary<CandidateId, CandidateSummary>> FindManyAsync(
        IReadOnlyCollection<CandidateId> candidateIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);
        if (candidateIds.Count == 0)
        {
            return new Dictionary<CandidateId, CandidateSummary>();
        }

        var found = await WithPositionState(dbContext.Candidates.Where(candidate => candidateIds.Contains(candidate.Id)))
            .ToListAsync(cancellationToken);
        return found.ToDictionary(item => item.Candidate.Id, item => Summarise(item.Candidate, item.State));
    }

    private IQueryable<CandidateWithPositionState> WithPositionState(IQueryable<Candidate> candidates) =>
        candidates.AsNoTracking().Join(
            dbContext.JobPositions,
            candidate => candidate.JobPositionId,
            position => position.Id,
            (candidate, position) => new CandidateWithPositionState(candidate, position.State));

    private static CandidateSummary Summarise(Candidate candidate, JobPositionState positionState) =>
        new(candidate.Id, candidate.JobPositionId, candidate.DisplayName, candidate.IsAnonymised, positionState == JobPositionState.Closed);

    private sealed record CandidateWithPositionState(Candidate Candidate, JobPositionState State);
}
