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
            .WithSummary(
                "Removes a task that hasn't started from a session's queue (409 InterviewSession.TaskStarted for a Current or Done "
                + "task, 409 InterviewSession.Finished once the session has finished).");
}
