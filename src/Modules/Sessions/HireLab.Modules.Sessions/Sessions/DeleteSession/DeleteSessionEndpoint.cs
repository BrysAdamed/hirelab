using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.DeleteSession;

internal static class DeleteSessionEndpoint
{
    public static RouteHandlerBuilder MapDeleteSession(this IEndpointRouteBuilder sessions) =>
        sessions.MapDelete("/{sessionId:guid}", async (
                InterviewSessionId sessionId,
                ICommandHandler<DeleteSessionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new DeleteSessionCommand(sessionId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeleteSession")
            .WithSummary("Deletes a planned session and its queue. A session that has started can't be deleted (409 InterviewSession.NotPlanned).");
}
