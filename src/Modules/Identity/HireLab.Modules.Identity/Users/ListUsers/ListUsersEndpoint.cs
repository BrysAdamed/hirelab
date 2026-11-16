using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Users.ListUsers;

internal static class ListUsersEndpoint
{
    public static RouteHandlerBuilder MapListUsers(this IEndpointRouteBuilder group, string name, Func<ICurrentUser, UserScope> scopeOf) =>
        group.MapGet(string.Empty, async (
                ICurrentUser currentUser,
                IQueryHandler<ListUsersQuery, Result<IReadOnlyList<UserResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListUsersQuery(scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName(name);
}
