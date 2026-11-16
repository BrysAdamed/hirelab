using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.GetJobPosition;

internal static class GetJobPositionEndpoint
{
    public static RouteHandlerBuilder MapGetJobPosition(this IEndpointRouteBuilder positions) =>
        positions.MapGet("/{positionId:guid}", async (
                JobPositionId positionId,
                IQueryHandler<GetJobPositionQuery, Result<JobPositionResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetJobPositionQuery(positionId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("GetJobPosition")
            .WithSummary("Gets one of the team's job positions with its version, which an edit sends back.");
}
