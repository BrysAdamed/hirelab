using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates.ListCandidates;

internal static class ListCandidatesEndpoint
{
    public static RouteHandlerBuilder MapListCandidates(this IEndpointRouteBuilder positionCandidates) =>
        positionCandidates.MapGet(string.Empty, async (
                JobPositionId positionId,
                IQueryHandler<ListCandidatesQuery, Result<IReadOnlyList<PositionCandidateResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListCandidatesQuery(positionId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListCandidates")
            .WithSummary("Lists a job position's candidates by number.");
}
