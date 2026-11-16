using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.CloseJobPosition;

internal sealed record CloseJobPositionRequest(uint Version);

internal static class CloseJobPositionEndpoint
{
    public static RouteHandlerBuilder MapCloseJobPosition(this IEndpointRouteBuilder positions) =>
        positions.MapPost("/{positionId:guid}/close", async (
                JobPositionId positionId,
                CloseJobPositionRequest request,
                ICommandHandler<CloseJobPositionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new CloseJobPositionCommand(positionId, request.Version), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("CloseJobPosition")
            .WithSummary(
                "Closes an open job position for good and anonymises all its candidates: contact details are cleared and "
                + "attachments deleted. Refused while a candidate's interview session isn't finished (409 "
                + "JobPosition.UnfinishedSessions) and when already closed (409 JobPosition.Closed). Send the version it "
                + "was read with (409 Concurrency.ChangedMeanwhile when it changed since).");
}
