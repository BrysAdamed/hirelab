using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.TaskBank.Tasks.GetTask;

internal static class GetTaskEndpoint
{
    public static RouteHandlerBuilder MapGetTask(this IEndpointRouteBuilder tasks) =>
        tasks.MapGet("/{taskId:guid}", async (
                TaskId taskId,
                IQueryHandler<GetTaskQuery, Result<TaskResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetTaskQuery(taskId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("GetTask")
            .WithSummary("Gets a task in the caller's task bank with its active version: metadata, reference answer, interviewer notes, rubric and content.");
}
