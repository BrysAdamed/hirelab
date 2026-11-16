using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.CandidateLinks.IssueCandidateLink;

internal static class IssueCandidateLinkEndpoint
{
    public static RouteHandlerBuilder MapIssueCandidateLink(this IEndpointRouteBuilder sessions) =>
        sessions.MapPost("/{sessionId:guid}/candidate-link", async (
                InterviewSessionId sessionId,
                ICommandHandler<IssueCandidateLinkCommand, Result<IssuedCandidateLinkResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new IssueCandidateLinkCommand(sessionId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("IssueCandidateLink")
            .WithSummary(
                "Creates the session's candidate link, or regenerates it, which revokes the old link and the candidate cookies "
                + "issued from it. Returns the link's path once: it can't be shown again. Refused for a finished session "
                + "(409 InterviewSession.Finished), a closed position (409 InterviewSession.PositionClosed) and an anonymised "
                + "candidate (409 InterviewSession.CandidateAnonymised).");
}
