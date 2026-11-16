using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.RescheduleSession;

internal sealed record RescheduleSessionRequest(DateTimeOffset? ScheduledAt);

internal static class RescheduleSessionEndpoint
{
    public static RouteHandlerBuilder MapRescheduleSession(this IEndpointRouteBuilder sessions) =>
        sessions.MapPut("/{sessionId:guid}/schedule", async (
                InterviewSessionId sessionId,
                RescheduleSessionRequest request,
                ICommandHandler<RescheduleSessionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RescheduleSessionCommand(sessionId, request.ScheduledAt), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("RescheduleSession")
            .WithSummary("Sets or clears when a planned session is scheduled. Refused once the session has started (409 InterviewSession.NotPlanned).");
}
