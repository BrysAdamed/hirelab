using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.CandidateAccess.ExchangeCandidateLink;

internal sealed record ExchangeCandidateLinkRequest(string Token)
{
    public override string ToString() => nameof(ExchangeCandidateLinkRequest);
}

/// <summary>
/// The anonymous endpoints the candidate app calls when a candidate link is opened (ADR 0008): an antiforgery token
/// first, then the rate-limited exchange of the link's token, sent in the body, for the candidate cookie.
/// </summary>
internal static class ExchangeCandidateLinkEndpoints
{
    public static void MapCandidateLinkExchange(this IEndpointRouteBuilder candidate)
    {
        candidate.MapGet("/access/antiforgery", IssueAntiforgeryToken)
            .AllowAnonymous()
            .WithName("GetCandidateAntiforgeryToken")
            .WithSummary("Issues the XSRF-TOKEN-CANDIDATE cookie for the current candidate, signed in or not.");

        candidate.MapPost("/access", ExchangeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("ExchangeCandidateLink")
            .WithSummary(
                "Exchanges a candidate link's token for the candidate cookie of its session and a new XSRF-TOKEN-CANDIDATE. Refused for "
                + "a link that doesn't work, for example because it was regenerated (404 CandidateLink.Invalid), and for a "
                + "finished session (409 CandidateSession.Ended).");
    }

    private static NoContent IssueAntiforgeryToken(HttpContext httpContext)
    {
        AntiforgeryTokens.IssueForCandidate(httpContext);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ExchangeAsync(
        ExchangeCandidateLinkRequest request,
        IQueryHandler<ExchangeCandidateLinkQuery, Result<CandidateCookie>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var exchanged = await handler.HandleAsync(new ExchangeCandidateLinkQuery(request.Token), cancellationToken);
        if (exchanged.IsFailure)
        {
            return exchanged.Error.ToProblem();
        }

        var candidate = exchanged.Value.ToPrincipal();
        await httpContext.SignInAsync(CandidateAuthentication.Scheme, candidate);
        httpContext.User = candidate;
        AntiforgeryTokens.IssueForCandidate(httpContext);
        return TypedResults.NoContent();
    }
}
