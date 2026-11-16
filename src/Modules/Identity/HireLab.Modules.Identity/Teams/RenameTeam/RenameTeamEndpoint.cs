using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Teams.RenameTeam;

internal sealed record RenameTeamRequest(string Name);

internal static class RenameTeamEndpoint
{
    public static RouteHandlerBuilder MapRenameTeam(this IEndpointRouteBuilder teams) =>
        teams.MapPost("/{teamId:guid}/rename", async (
                TeamId teamId,
                RenameTeamRequest request,
                ICommandHandler<RenameTeamCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RenameTeamCommand(teamId, request.Name), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("RenameTeam")
            .WithSummary("Renames a team. Its id, members and data stay the same.");
}
