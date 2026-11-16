using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.ListCandidateSessions;

internal static class ListCandidateSessionsEndpoint
{
    public static RouteHandlerBuilder MapListCandidateSessions(this IEndpointRouteBuilder candidateSessions) =>
        candidateSessions.MapGet(string.Empty, async (
                CandidateId candidateId,
                IQueryHandler<ListCandidateSessionsQuery, Result<IReadOnlyList<CandidateSessionResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListCandidateSessionsQuery(candidateId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListCandidateSessions")
            .WithSummary("Lists a candidate's interview sessions by number.");
}
