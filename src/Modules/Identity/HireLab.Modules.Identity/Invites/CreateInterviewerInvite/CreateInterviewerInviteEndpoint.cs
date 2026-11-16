using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.CreateInterviewerInvite;

internal sealed record CreateInterviewerInviteRequest(string Email, string DisplayName);

internal static class CreateInterviewerInviteEndpoint
{
    public static RouteHandlerBuilder MapCreateInterviewerInvite(this IEndpointRouteBuilder interviewers) =>
        interviewers.MapPost(string.Empty, HandleAsync)
            .WithName("CreateInterviewerInvite")
            .WithSummary(
                "Invites an interviewer into the manager's team. Revokes any open invite for the same email and returns the "
                + "link, which is shown only once.");

    private static async Task<Results<Ok<IssuedInviteResponse>, ProblemHttpResult>> HandleAsync(
        CreateInterviewerInviteRequest request,
        ICommandHandler<CreateInterviewerInviteCommand, Result<IssuedInviteResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateInterviewerInviteCommand(request.Email, request.DisplayName), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
