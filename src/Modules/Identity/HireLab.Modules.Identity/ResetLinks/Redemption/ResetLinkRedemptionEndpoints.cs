using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.ResetLinks.Redemption;

internal sealed record PreviewResetLinkRequest(string Token);

internal sealed record ResetPasswordRequest(string Token, string Password);

/// <summary>The anonymous, rate-limited endpoints of the reset page. The token comes in the body, never the URL.</summary>
internal static class ResetLinkRedemptionEndpoints
{
    public static void MapResetLinkRedemption(this IEndpointRouteBuilder resetLinks)
    {
        resetLinks.MapPost("/preview", PreviewAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("PreviewResetLink")
            .WithSummary("Returns whose password a pending reset link sets.");

        resetLinks.MapPost("/redeem", RedeemAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("RedeemResetLink")
            .WithSummary(
                "Redeems a reset link with the user's new password: clears the lockout and ends the user's existing sessions. "
                + "The user then logs in with the new password.");
    }

    private static async Task<Results<Ok<ResetLinkPreviewResponse>, ProblemHttpResult>> PreviewAsync(
        PreviewResetLinkRequest request,
        IQueryHandler<PreviewResetLinkQuery, Result<ResetLinkPreviewResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PreviewResetLinkQuery(request.Token), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RedeemAsync(
        ResetPasswordRequest request,
        ICommandHandler<ResetPasswordCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResetPasswordCommand(request.Token, request.Password), cancellationToken);
        return result.ToHttpResult();
    }
}
