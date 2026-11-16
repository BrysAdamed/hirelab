using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.DeleteJobPosition;

internal static class DeleteJobPositionEndpoint
{
    public static RouteHandlerBuilder MapDeleteJobPosition(this IEndpointRouteBuilder positions) =>
        positions.MapDelete("/{positionId:guid}", async (
                JobPositionId positionId,
                ICommandHandler<DeleteJobPositionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new DeleteJobPositionCommand(positionId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeleteJobPosition")
            .WithSummary("Deletes a job position. Only an open position without candidates can be deleted.");
}
