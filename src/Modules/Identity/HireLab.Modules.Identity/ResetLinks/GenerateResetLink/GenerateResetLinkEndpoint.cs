using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.ResetLinks.GenerateResetLink;

internal static class GenerateResetLinkEndpoint
{
    public static RouteHandlerBuilder MapGenerateResetLink(
        this IEndpointRouteBuilder group,
        string name,
        Func<ICurrentUser, ResetLinkScope> scopeOf) =>
        group.MapPost("/{userId:guid}", async Task<Results<Ok<IssuedResetLinkResponse>, ProblemHttpResult>> (
                UserId userId,
                ICurrentUser currentUser,
                ICommandHandler<GenerateResetLinkCommand, Result<IssuedResetLinkResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GenerateResetLinkCommand(userId, scopeOf(currentUser)), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName(name);
}
