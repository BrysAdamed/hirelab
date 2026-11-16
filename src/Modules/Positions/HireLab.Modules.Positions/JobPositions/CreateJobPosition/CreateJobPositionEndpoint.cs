using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.JobPositions.CreateJobPosition;

internal sealed record CreateJobPositionRequest(
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes);

internal static class CreateJobPositionEndpoint
{
    public static RouteHandlerBuilder MapCreateJobPosition(this IEndpointRouteBuilder positions) =>
        positions.MapPost(string.Empty, HandleAsync)
            .WithName("CreateJobPosition")
            .WithSummary("Creates an open job position in the manager's team.");

    private static async Task<Results<Created<CreatedJobPositionResponse>, ProblemHttpResult>> HandleAsync(
        CreateJobPositionRequest request,
        ICommandHandler<CreateJobPositionCommand, Result<CreatedJobPositionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var details = new JobPositionDetails(request.Title, request.Description, request.Requirements, request.NiceToHaves, request.InterviewerNotes);
        var result = await handler.HandleAsync(new CreateJobPositionCommand(details), cancellationToken);
        return result.ToHttpResult(created => TypedResults.Created($"/api/staff/positions/{created.Id}", created));
    }
}
