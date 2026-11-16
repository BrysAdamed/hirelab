using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Users.ReactivateUser;

internal static class ReactivateUserEndpoint
{
    public static RouteHandlerBuilder MapReactivateUser(this IEndpointRouteBuilder group, string name, Func<ICurrentUser, UserScope> scopeOf) =>
        group.MapPost("/{userId:guid}/reactivate", async (
                UserId userId,
                ICurrentUser currentUser,
                ICommandHandler<ReactivateUserCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ReactivateUserCommand(userId, scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName(name)
            .WithSummary("Reactivates a deactivated user. A manager is refused while their team has another active manager.");
}
