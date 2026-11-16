namespace HireLab.Modules.Positions.Contracts;

/// <summary>
/// What another module may show about a candidate (ADR 0023). Never copy <see cref="DisplayName"/> into another
/// module's storage: read it again when it is needed, so anonymisation reaches every screen.
/// </summary>
/// <param name="Id">The candidate.</param>
/// <param name="JobPositionId">The job position the candidate applied to.</param>
/// <param name="DisplayName">The candidate's full name, or "Candidate #N" once they are anonymised.</param>
/// <param name="IsAnonymised">Whether the candidate's personal data has been removed.</param>
/// <param name="IsJobPositionClosed">Whether the candidate's job position is closed, so they can't be interviewed any more.</param>
public sealed record CandidateSummary(
    CandidateId Id,
    JobPositionId JobPositionId,
    string DisplayName,
    bool IsAnonymised,
    bool IsJobPositionClosed);
