using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Queue.RemoveSessionTask;

internal static class RemoveSessionTaskEndpoint
{
    public static RouteHandlerBuilder MapRemoveSessionTask(this IEndpointRouteBuilder queue) =>
        queue.MapDelete("/{sessionTaskId:guid}", async (
                InterviewSessionId sessionId,
                SessionTaskId sessionTaskId,
                ICommandHandler<RemoveSessionTaskCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RemoveSessionTaskCommand(sessionId, sessionTaskId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("RemoveSessionTask")
            .WithSummary("Removes a task from a planned session's queue.");
}
