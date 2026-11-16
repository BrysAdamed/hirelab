using HireLab.Modules.Identity.Auth.CurrentUser;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites.Redemption;

internal sealed record PreviewInviteRequest(string Token);

internal sealed record RedeemInviteRequest(string Token, string Password);

/// <summary>The anonymous, rate-limited endpoints of the redemption page. The token comes in the body, never the URL.</summary>
internal static class RedemptionEndpoints
{
    public static void MapInviteRedemption(this IEndpointRouteBuilder invites)
    {
        invites.MapPost("/preview", PreviewAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("PreviewInvite")
            .WithSummary("Returns the invitee, role and team of a pending invite link.");

        invites.MapPost("/redeem", RedeemAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("RedeemInvite")
            .WithSummary(
                "Redeems an invite link with the invitee's new password: creates the user (and the team of a new-team invite), "
                + "logs them in with a new XSRF-TOKEN and returns the user.");
    }

    private static async Task<Results<Ok<InvitePreviewResponse>, ProblemHttpResult>> PreviewAsync(
        PreviewInviteRequest request,
        IQueryHandler<PreviewInviteQuery, Result<InvitePreviewResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PreviewInviteQuery(request.Token), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }

    private static async Task<Results<Ok<CurrentUserResponse>, ProblemHttpResult>> RedeemAsync(
        RedeemInviteRequest request,
        ICommandHandler<RedeemInviteCommand, Result<UserId>> redeem,
        IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>> getCurrentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var redeemed = await redeem.HandleAsync(new RedeemInviteCommand(request.Token, request.Password), cancellationToken);
        if (redeemed.IsFailure)
        {
            return redeemed.Error.ToProblem();
        }

        AntiforgeryTokens.Issue(httpContext);

        var currentUser = await getCurrentUser.HandleAsync(new GetCurrentUserQuery(redeemed.Value), cancellationToken);
        return currentUser.ToHttpResult(TypedResults.Ok);
    }
}
