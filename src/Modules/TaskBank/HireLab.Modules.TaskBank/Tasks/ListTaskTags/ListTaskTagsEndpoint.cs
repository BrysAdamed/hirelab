using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.TaskBank.Tasks.ListTaskTags;

internal static class ListTaskTagsEndpoint
{
    public static RouteHandlerBuilder MapListTaskTags(this IEndpointRouteBuilder tasks) =>
        tasks.MapGet("/tags", async (
                IQueryHandler<ListTaskTagsQuery, Result<IReadOnlyList<string>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListTaskTagsQuery(), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTaskTags")
            .WithSummary("Lists the distinct tags of the tasks in the caller's task bank, alphabetically.");
}
