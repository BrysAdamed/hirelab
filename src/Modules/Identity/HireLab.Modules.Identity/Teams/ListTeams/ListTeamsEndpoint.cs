using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Teams.ListTeams;

internal static class ListTeamsEndpoint
{
    public static RouteHandlerBuilder MapListTeams(this IEndpointRouteBuilder teams) =>
        teams.MapGet(string.Empty, async (
                IQueryHandler<ListTeamsQuery, Result<IReadOnlyList<TeamResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListTeamsQuery(), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTeams")
            .WithSummary("Lists every team with its active manager (or none) and its members.");
}
