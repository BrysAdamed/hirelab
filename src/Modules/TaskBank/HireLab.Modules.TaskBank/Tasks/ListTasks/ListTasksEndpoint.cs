using HireLab.Modules.TaskBank.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.TaskBank.Tasks.ListTasks;

internal static class ListTasksEndpoint
{
    public static RouteHandlerBuilder MapListTasks(this IEndpointRouteBuilder tasks) =>
        tasks.MapGet(string.Empty, async (
                string[]? tag,
                TaskLevel[]? level,
                string? taskType,
                string? search,
                IQueryHandler<ListTasksQuery, Result<IReadOnlyList<TaskSummaryResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new ListTasksQuery(tag ?? [], level ?? [], taskType, search);
                var result = await handler.HandleAsync(query, cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTasks")
            .WithSummary(
                "Lists the task bank: the active version of every task the caller can see, by title. A task has every given tag, "
                + "one of the given levels, the given task type, and the search text in its title or summary.");
}
