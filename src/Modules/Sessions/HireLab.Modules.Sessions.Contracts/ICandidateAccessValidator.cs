using System.Security.Claims;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Checks a candidate cookie on every candidate request (ADR 0008): its session still exists and isn't
/// <c>Finished</c>, and its link hasn't been regenerated since the cookie was issued. The API host calls it when it
/// validates the candidate cookie and rejects the cookie on a failure, answering 401 with the failure's code; the
/// candidate hub calls it when a connection opens and again when the session changes, and drops connections that fail.
/// Registered as a scoped service by the Sessions module.
/// </summary>
public interface ICandidateAccessValidator
{
    /// <summary>
    /// The session and link that <paramref name="candidate"/>, the candidate cookie's principal, gives access to, or the
    /// reason it no longer does.
    /// </summary>
    Task<Result<CandidateSessionAccess>> ValidateAsync(ClaimsPrincipal candidate, CancellationToken cancellationToken = default);
}
