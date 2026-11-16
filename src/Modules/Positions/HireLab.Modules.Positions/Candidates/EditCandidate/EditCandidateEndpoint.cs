using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates.EditCandidate;

internal sealed record EditCandidateRequest(string FullName, string? Email, string? Phone, IReadOnlyList<string>? Links, uint Version);

internal static class EditCandidateEndpoint
{
    public static RouteHandlerBuilder MapEditCandidate(this IEndpointRouteBuilder candidates) =>
        candidates.MapPut("/{candidateId:guid}", async (
                CandidateId candidateId,
                EditCandidateRequest request,
                ICommandHandler<EditCandidateCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var details = new CandidateDetails(request.FullName, request.Email, request.Phone, request.Links ?? []);
                var result = await handler.HandleAsync(new EditCandidateCommand(candidateId, details, request.Version), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("EditCandidate")
            .WithSummary(
                "Edits a candidate's contact details while their position is open (409 JobPosition.Closed otherwise). Send "
                + "the version it was read with: when someone else changed it since, the edit is refused with 409 "
                + "Concurrency.ChangedMeanwhile.");
}
