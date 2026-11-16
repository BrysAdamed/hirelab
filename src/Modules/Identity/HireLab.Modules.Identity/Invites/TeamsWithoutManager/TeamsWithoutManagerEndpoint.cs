using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.TeamsWithoutManager;

internal static class TeamsWithoutManagerEndpoint
{
    public static RouteHandlerBuilder MapTeamsWithoutManager(this IEndpointRouteBuilder managers) =>
        managers.MapGet("/teams-without-manager", async (
                IQueryHandler<TeamsWithoutManagerQuery, Result<IReadOnlyList<TeamWithoutManagerResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new TeamsWithoutManagerQuery(), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTeamsWithoutManager")
            .WithSummary("Lists the teams without an active manager, which a manager invite can hand over.");
}
