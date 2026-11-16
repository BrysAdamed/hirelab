using System.Security.Claims;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.CandidateAccess;

/// <summary>
/// Checks the candidate cookie on every candidate request (ADR 0008): one lookup of the session by both of the cookie's
/// ids. A deleted session or a regenerated link fails with <c>CandidateLink.Invalid</c>, a finished session with
/// <c>CandidateSession.Ended</c>.
/// </summary>
internal sealed class CandidateAccessValidator(CandidateSessions sessions) : ICandidateAccessValidator
{
    public async Task<Result<CandidateSessionAccess>> ValidateAsync(ClaimsPrincipal candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (CandidateCookie.From(candidate) is not { } cookie)
        {
            return CandidateAccessErrors.InvalidLink;
        }

        var access = CandidateSessions.CheckAccess(await sessions.FindStateAsync(cookie, cancellationToken));
        return access.IsSuccess ? new CandidateSessionAccess(cookie.SessionId, cookie.CandidateLinkId) : access.Error;
    }
}
