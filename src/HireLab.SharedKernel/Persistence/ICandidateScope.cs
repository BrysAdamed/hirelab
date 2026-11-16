namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Marks the code that reads a candidate's own interview session for the candidate link and the candidate cookie
/// (ADR 0008, ADR 0031). Candidates are scoped by their session, not by a team (ADR 0009), so such a type may bypass the
/// <see cref="TeamQueryFilter"/> with <c>IgnoreQueryFilters([TeamQueryFilter.Name])</c>, and must itself filter by the
/// link token's hash or by the session and link ids from the cookie's claims. An architecture test allows the bypass
/// only here, in system jobs and in the migration worker.
/// </summary>
public interface ICandidateScope;
