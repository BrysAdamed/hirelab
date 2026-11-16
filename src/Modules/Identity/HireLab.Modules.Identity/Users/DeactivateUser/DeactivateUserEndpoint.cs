using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Users.DeactivateUser;

internal static class DeactivateUserEndpoint
{
    public static RouteHandlerBuilder MapDeactivateUser(this IEndpointRouteBuilder group, string name, Func<ICurrentUser, StaffScope> scopeOf) =>
        group.MapPost("/{userId:guid}/deactivate", async (
                UserId userId,
                ICurrentUser currentUser,
                ICommandHandler<DeactivateUserCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new DeactivateUserCommand(userId, scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName(name)
            .WithSummary(
                "Deactivates a user: they can't log in, their sessions end within 5 minutes and the pending invites they issued "
                + "are revoked. Nobody can deactivate themselves.");
}
