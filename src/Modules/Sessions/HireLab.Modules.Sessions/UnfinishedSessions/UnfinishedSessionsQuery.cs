using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;

namespace HireLab.Modules.Sessions.UnfinishedSessions;

internal sealed class UnfinishedSessionsQuery : IUnfinishedSessionsQuery
{
    // TODO HLB-6: query the candidates' sessions once interview sessions exist; until then no candidate has one.
    public Task<bool> AnyForCandidatesAsync(IReadOnlyCollection<CandidateId> candidateIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateIds);
        return Task.FromResult(false);
    }
}
