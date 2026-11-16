using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Running.MarkSessionTaskDone;

internal static class MarkSessionTaskDoneEndpoint
{
    public static RouteHandlerBuilder MapMarkSessionTaskDone(this IEndpointRouteBuilder queue) =>
        queue.MapPost("/{sessionTaskId:guid}/done", async (
                InterviewSessionId sessionId,
                SessionTaskId sessionTaskId,
                ICommandHandler<MarkSessionTaskDoneCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new MarkSessionTaskDoneCommand(sessionId, sessionTaskId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("MarkSessionTaskDone")
            .WithSummary(
                "Marks a live session's Current task done; the next Pending task becomes Current, re-pinned to its task's active "
                + "version. Refused for a session that isn't Live (409 InterviewSession.NotLive) and a task that isn't Current "
                + "(409 InterviewSession.TaskNotCurrent).");
}
