using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.EditJobPosition;

internal sealed record EditJobPositionRequest(
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes,
    uint Version);

internal static class EditJobPositionEndpoint
{
    public static RouteHandlerBuilder MapEditJobPosition(this IEndpointRouteBuilder positions) =>
        positions.MapPut("/{positionId:guid}", async (
                JobPositionId positionId,
                EditJobPositionRequest request,
                ICommandHandler<EditJobPositionCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var details = new JobPositionDetails(request.Title, request.Description, request.Requirements, request.NiceToHaves, request.InterviewerNotes);
                var result = await handler.HandleAsync(new EditJobPositionCommand(positionId, details, request.Version), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("EditJobPosition")
            .WithSummary(
                "Edits an open job position. Send the version it was read with: when someone else changed it since, the edit "
                + "is refused with 409 Concurrency.ChangedMeanwhile. A closed position is read-only (409 JobPosition.Closed).");
}
