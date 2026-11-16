using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.UnfinishedSessions;

internal sealed class UnfinishedSessionsQuery(SessionsDbContext dbContext) : IUnfinishedSessionsQuery
{
    public async Task<bool> AnyForCandidatesAsync(IReadOnlyCollection<CandidateId> candidateIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);
        if (candidateIds.Count == 0)
        {
            return false;
        }

        return await dbContext.Sessions.AnyAsync(
            session => candidateIds.Contains(session.CandidateId) && session.State != InterviewSessionState.Finished,
            cancellationToken);
    }
}
