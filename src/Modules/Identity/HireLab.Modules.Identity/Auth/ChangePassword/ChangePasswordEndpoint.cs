using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth.ChangePassword;

internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

internal static class ChangePasswordEndpoint
{
    public static RouteHandlerBuilder MapChangePassword(this IEndpointRouteBuilder auth) =>
        auth.MapPost("/change-password", HandleAsync)
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("ChangePassword")
            .WithSummary("Changes the signed-in user's password. Their other sessions end; this one continues.");

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        ChangePasswordRequest request,
        ICommandHandler<ChangePasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return result.ToHttpResult();
    }
}
