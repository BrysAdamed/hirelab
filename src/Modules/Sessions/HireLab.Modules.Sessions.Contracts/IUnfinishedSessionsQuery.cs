using HireLab.Modules.Positions.Contracts;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Tells other modules whether candidates still have an unfinished interview session (one that is not
/// <c>Finished</c>). Positions asks it before closing a job position or anonymising a candidate, which are refused
/// while a session is unfinished. Registered as a scoped service by the Sessions module.
/// </summary>
public interface IUnfinishedSessionsQuery
{
    /// <summary>Whether any candidate in <paramref name="candidateIds"/> has a session that is not finished.</summary>
    Task<bool> AnyForCandidatesAsync(IReadOnlyCollection<CandidateId> candidateIds, CancellationToken cancellationToken = default);
}
