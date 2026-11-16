using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Running.GoLive;

internal static class GoLiveEndpoint
{
    public static RouteHandlerBuilder MapGoLive(this IEndpointRouteBuilder sessions) =>
        sessions.MapPost("/{sessionId:guid}/go-live", async (
                InterviewSessionId sessionId,
                ICommandHandler<GoLiveCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GoLiveCommand(sessionId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("GoLive")
            .WithSummary(
                "Starts a planned session: its first task becomes Current, re-pinned to its task's active version. Refused for a "
                + "session that has started (409 InterviewSession.NotPlanned), an empty queue (409 InterviewSession.EmptyQueue), "
                + "a closed position (409 InterviewSession.PositionClosed) and an anonymised candidate "
                + "(409 InterviewSession.CandidateAnonymised).");
}
