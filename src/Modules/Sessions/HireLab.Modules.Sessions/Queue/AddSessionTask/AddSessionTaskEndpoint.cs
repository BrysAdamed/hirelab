using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Queue.AddSessionTask;

internal sealed record AddSessionTaskRequest(Guid TaskId);

internal static class AddSessionTaskEndpoint
{
    public static RouteHandlerBuilder MapAddSessionTask(this IEndpointRouteBuilder queue) =>
        queue.MapPost(string.Empty, HandleAsync)
            .WithName("AddSessionTask")
            .WithSummary(
                "Adds a task from the task bank to the end of a planned session's queue, pinning its active version. "
                + "A task is queued at most once (409 InterviewSession.TaskAlreadyQueued).");

    private static async Task<Results<Created<AddedSessionTaskResponse>, ProblemHttpResult>> HandleAsync(
        InterviewSessionId sessionId,
        AddSessionTaskRequest request,
        ICommandHandler<AddSessionTaskCommand, Result<AddedSessionTaskResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AddSessionTaskCommand(sessionId, new TaskId(request.TaskId)), cancellationToken);
        return result.ToHttpResult(added => TypedResults.Created($"/api/staff/sessions/{sessionId}", added));
    }
}
