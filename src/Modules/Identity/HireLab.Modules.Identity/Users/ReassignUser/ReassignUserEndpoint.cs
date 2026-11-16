using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Users.ReassignUser;

internal sealed record ReassignUserRequest(Guid TeamId);

internal static class ReassignUserEndpoint
{
    public static RouteHandlerBuilder MapReassignUser(this IEndpointRouteBuilder users) =>
        users.MapPost("/{userId:guid}/reassign", async (
                UserId userId,
                ReassignUserRequest request,
                ICommandHandler<ReassignUserCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ReassignUserCommand(userId, request.TeamId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("ReassignUser")
            .WithSummary(
                "Moves an interviewer to another team. Their sessions end within 5 minutes, so they lose the old team's data. "
                + "Invites are not affected.");
}
