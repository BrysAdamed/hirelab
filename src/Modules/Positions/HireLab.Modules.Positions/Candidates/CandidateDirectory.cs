using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates;

internal sealed class CandidateDirectory(PositionsDbContext dbContext) : ICandidateDirectory
{
    public async Task<CandidateSummary?> FindAsync(CandidateId candidateId, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Candidates.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == candidateId, cancellationToken);
        return candidate is null ? null : Summarise(candidate);
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

        var candidates = await dbContext.Candidates.AsNoTracking()
            .Where(candidate => candidateIds.Contains(candidate.Id))
            .ToListAsync(cancellationToken);
        return candidates.ToDictionary(candidate => candidate.Id, Summarise);
    }

    private static CandidateSummary Summarise(Candidate candidate) =>
        new(candidate.Id, candidate.JobPositionId, candidate.DisplayName, candidate.IsAnonymised);
}
