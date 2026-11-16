using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.TaskBank.Tasks.ListTaskTypes;

internal static class ListTaskTypesEndpoint
{
    public static RouteHandlerBuilder MapListTaskTypes(this IEndpointRouteBuilder tasks) =>
        tasks.MapGet("/types", async (
                IQueryHandler<ListTaskTypesQuery, Result<IReadOnlyList<string>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListTaskTypesQuery(), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTaskTypes")
            .WithSummary("Lists the keys of the task types that tasks in the caller's task bank have, alphabetically.");
}
