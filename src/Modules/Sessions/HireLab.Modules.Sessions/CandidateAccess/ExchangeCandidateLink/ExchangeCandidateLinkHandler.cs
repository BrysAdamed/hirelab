using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.CandidateAccess.ExchangeCandidateLink;

internal sealed record ExchangeCandidateLinkQuery(string Token)
{
    public override string ToString() => nameof(ExchangeCandidateLinkQuery);
}

/// <summary>
/// Finds the session a candidate link opens (ADR 0008) and returns what the candidate cookie carries. An unknown token,
/// a regenerated link and a deleted session fail alike with <c>CandidateLink.Invalid</c>; a finished session fails with
/// <c>CandidateSession.Ended</c>. Planned and Live sessions give access.
/// </summary>
internal sealed class ExchangeCandidateLinkHandler(CandidateSessions sessions)
    : IQueryHandler<ExchangeCandidateLinkQuery, Result<CandidateCookie>>
{
    public async Task<Result<CandidateCookie>> HandleAsync(ExchangeCandidateLinkQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(query.Token) || query.Token.Length > CandidateLinkToken.MaxLength)
        {
            return CandidateAccessErrors.InvalidLink;
        }

        var session = await sessions.FindByTokenAsync(query.Token, cancellationToken);
        var access = CandidateSessions.CheckAccess(session?.State);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return new CandidateCookie(session!.SessionId, session.CandidateLinkId);
    }
}
