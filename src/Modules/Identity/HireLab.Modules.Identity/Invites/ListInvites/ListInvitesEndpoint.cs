using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.ListInvites;

internal static class ListInvitesEndpoint
{
    public static RouteHandlerBuilder MapListInvites(this IEndpointRouteBuilder group, string name, Func<ICurrentUser, StaffScope> scopeOf) =>
        group.MapGet(string.Empty, async (
                ICurrentUser currentUser,
                IQueryHandler<ListInvitesQuery, Result<IReadOnlyList<InviteResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListInvitesQuery(scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName(name);
}
