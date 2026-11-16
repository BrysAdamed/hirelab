using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates.AddCandidate;

internal sealed record AddCandidateRequest(string FullName, string? Email, string? Phone, IReadOnlyList<string>? Links);

internal static class AddCandidateEndpoint
{
    public static RouteHandlerBuilder MapAddCandidate(this IEndpointRouteBuilder positionCandidates) =>
        positionCandidates.MapPost(string.Empty, HandleAsync)
            .WithName("AddCandidate")
            .WithSummary("Adds a candidate to an open job position. The candidate gets the position's next number.");

    private static async Task<Results<Created<AddedCandidateResponse>, ProblemHttpResult>> HandleAsync(
        JobPositionId positionId,
        AddCandidateRequest request,
        ICommandHandler<AddCandidateCommand, Result<AddedCandidateResponse>> handler,
        CancellationToken cancellationToken)
    {
        var details = new CandidateDetails(request.FullName, request.Email, request.Phone, request.Links ?? []);
        var result = await handler.HandleAsync(new AddCandidateCommand(positionId, details), cancellationToken);
        return result.ToHttpResult(added => TypedResults.Created($"/api/staff/candidates/{added.Id}", added));
    }
}
