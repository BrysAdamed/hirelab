using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.ListJobPositions;

internal static class ListJobPositionsEndpoint
{
    public static RouteHandlerBuilder MapListJobPositions(this IEndpointRouteBuilder positions) =>
        positions.MapGet(string.Empty, async (
                JobPositionState? state,
                IQueryHandler<ListJobPositionsQuery, Result<IReadOnlyList<JobPositionSummaryResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListJobPositionsQuery(state ?? JobPositionState.Open), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListJobPositions")
            .WithSummary("Lists the team's job positions in one state (Open by default), newest first.");
}
