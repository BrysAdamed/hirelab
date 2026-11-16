using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.CreateManagerInvite;

internal sealed record CreateManagerInviteRequest(string Email, string DisplayName, Guid? TeamId, string? NewTeamName);

internal static class CreateManagerInviteEndpoint
{
    public static RouteHandlerBuilder MapCreateManagerInvite(this IEndpointRouteBuilder managers) =>
        managers.MapPost(string.Empty, HandleAsync)
            .WithName("CreateManagerInvite")
            .WithSummary(
                "Invites a manager into a new team (newTeamName) or an existing team without an active manager (teamId). "
                + "Replaces the admin's open invite for the same email (a pending invite someone else manages is a conflict) and "
                + "returns the link, which is shown only once.");

    private static async Task<Results<Ok<IssuedInviteResponse>, ProblemHttpResult>> HandleAsync(
        CreateManagerInviteRequest request,
        ICommandHandler<CreateManagerInviteCommand, Result<IssuedInviteResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateManagerInviteCommand(request.Email, request.DisplayName, request.TeamId, request.NewTeamName),
            cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
