using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Queue.ReorderSessionTasks;

internal sealed record ReorderSessionTasksRequest(IReadOnlyList<Guid> SessionTaskIds);

internal static class ReorderSessionTasksEndpoint
{
    public static RouteHandlerBuilder MapReorderSessionTasks(this IEndpointRouteBuilder queue) =>
        queue.MapPut("/order", async (
                InterviewSessionId sessionId,
                ReorderSessionTasksRequest request,
                ICommandHandler<ReorderSessionTasksCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var order = (request.SessionTaskIds ?? []).Select(id => new SessionTaskId(id)).ToList();
                var result = await handler.HandleAsync(new ReorderSessionTasksCommand(sessionId, order), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ReorderSessionTasks")
            .WithSummary(
                "Puts a session's queue in the given order. It must list every task in the queue exactly once and keep the tasks "
                + "that have started in their places (409 InterviewSession.TaskStarted); "
                + "a stale list is refused with 409 InterviewSession.OrderMismatch.");
}
