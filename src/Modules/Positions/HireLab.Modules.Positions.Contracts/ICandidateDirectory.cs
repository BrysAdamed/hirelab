namespace HireLab.Modules.Positions.Contracts;

/// <summary>
/// Looks up candidates for other modules (ADR 0023). Registered as a scoped service by the Positions module. Like
/// every team-owned read (ADR 0009), it sees only the current staff user's team: another team's candidate, and any
/// candidate for the admin, a candidate's own request or code outside a request, is not found.
/// </summary>
public interface ICandidateDirectory
{
    /// <summary>The candidate's summary, or <c>null</c> when the current team has no such candidate.</summary>
    Task<CandidateSummary?> FindAsync(CandidateId candidateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The summaries of the candidates in <paramref name="candidateIds"/> that the current team has, keyed by id. Ids it
    /// doesn't find are missing from the result.
    /// </summary>
    Task<IReadOnlyDictionary<CandidateId, CandidateSummary>> FindManyAsync(
        IReadOnlyCollection<CandidateId> candidateIds,
        CancellationToken cancellationToken = default);
}
