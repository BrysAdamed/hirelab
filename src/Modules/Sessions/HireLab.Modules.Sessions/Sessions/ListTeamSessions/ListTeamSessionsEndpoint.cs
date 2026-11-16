using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.ListTeamSessions;

internal static class ListTeamSessionsEndpoint
{
    public static RouteHandlerBuilder MapListTeamSessions(this IEndpointRouteBuilder sessions) =>
        sessions.MapGet(string.Empty, async (
                IQueryHandler<ListTeamSessionsQuery, Result<IReadOnlyList<TeamSessionResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListTeamSessionsQuery(), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListTeamSessions")
            .WithSummary("Lists the team's Planned and Live sessions, soonest first: Live ones, then by schedule, unscheduled ones last.");
}
