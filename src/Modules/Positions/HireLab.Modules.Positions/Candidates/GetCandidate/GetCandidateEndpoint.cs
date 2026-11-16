using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates.GetCandidate;

internal static class GetCandidateEndpoint
{
    public static RouteHandlerBuilder MapGetCandidate(this IEndpointRouteBuilder candidates) =>
        candidates.MapGet("/{candidateId:guid}", async (
                CandidateId candidateId,
                IQueryHandler<GetCandidateQuery, Result<CandidateResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetCandidateQuery(candidateId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("GetCandidate")
            .WithSummary("Gets one of the team's candidates with their contact details and version, which an edit sends back.");
}
