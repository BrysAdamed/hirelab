using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.RevokeInvite;

internal static class RevokeInviteEndpoint
{
    public static RouteHandlerBuilder MapRevokeInvite(this IEndpointRouteBuilder group, string name, Func<ICurrentUser, StaffScope> scopeOf) =>
        group.MapPost("/{inviteId:guid}/revoke", async (
                InviteId inviteId,
                ICurrentUser currentUser,
                ICommandHandler<RevokeInviteCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RevokeInviteCommand(inviteId, scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName(name)
            .WithSummary("Revokes an invite that hasn't been used, so its link stops working.");
}
