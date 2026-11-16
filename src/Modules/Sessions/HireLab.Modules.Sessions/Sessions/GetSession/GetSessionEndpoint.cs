using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.GetSession;

internal static class GetSessionEndpoint
{
    public static RouteHandlerBuilder MapGetSession(this IEndpointRouteBuilder sessions) =>
        sessions.MapGet("/{sessionId:guid}", async (
                InterviewSessionId sessionId,
                IQueryHandler<GetSessionQuery, Result<SessionResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetSessionQuery(sessionId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("GetSession")
            .WithSummary(
                "Gets one of the team's sessions with its queue in order. Pending tasks whose task has another active version "
                + "are re-pinned to it first.");
}
