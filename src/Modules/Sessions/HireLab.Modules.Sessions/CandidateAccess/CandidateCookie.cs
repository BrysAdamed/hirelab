using System.Security.Claims;
using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Sessions.CandidateAccess;

/// <summary>
/// What the candidate cookie carries (ADR 0008): the claims <c>sessionId</c> and <c>candidateLinkId</c>. Candidate
/// endpoints take the session only from here, never from the route.
/// </summary>
internal sealed record CandidateCookie(InterviewSessionId SessionId, CandidateLinkId CandidateLinkId)
{
    public const string SessionIdClaim = "sessionId";
    public const string CandidateLinkIdClaim = "candidateLinkId";

    /// <summary>The claims of a candidate cookie, or <c>null</c> when <paramref name="principal"/> isn't a candidate's.</summary>
    public static CandidateCookie? From(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Identity is { IsAuthenticated: true, AuthenticationType: CandidateAuthentication.Scheme }
            && InterviewSessionId.TryParse(principal.FindFirstValue(SessionIdClaim), null, out var sessionId)
            && CandidateLinkId.TryParse(principal.FindFirstValue(CandidateLinkIdClaim), null, out var candidateLinkId)
                ? new CandidateCookie(sessionId, candidateLinkId)
                : null;
    }

    public ClaimsPrincipal ToPrincipal() =>
        new(new ClaimsIdentity(
            [new Claim(SessionIdClaim, SessionId.ToString()), new Claim(CandidateLinkIdClaim, CandidateLinkId.ToString())],
            CandidateAuthentication.Scheme));
}
