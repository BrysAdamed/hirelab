using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Running.FinishSession;

internal static class FinishSessionEndpoint
{
    public static RouteHandlerBuilder MapFinishSession(this IEndpointRouteBuilder sessions) =>
        sessions.MapPost("/{sessionId:guid}/finish", async (
                InterviewSessionId sessionId,
                ICommandHandler<FinishSessionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new FinishSessionCommand(sessionId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("FinishSession")
            .WithSummary(
                "Finishes a live session for good: the Current task becomes Done and Pending tasks stay Pending (not reached). "
                + "Refused for a session that isn't Live (409 InterviewSession.NotLive).");
}
