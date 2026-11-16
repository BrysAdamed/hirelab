using HireLab.Modules.Identity.Auth.CurrentUser;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth.Login;

internal sealed record LoginRequest(string Email, string Password);

internal static class LoginEndpoint
{
    public static RouteHandlerBuilder MapLogin(this IEndpointRouteBuilder auth) =>
        auth.MapPost("/login", HandleAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication)
            .WithName("Login")
            .WithSummary("Logs in with email and password, sets the staff cookie and a new XSRF-TOKEN, and returns the user.");

    private static async Task<Results<Ok<CurrentUserResponse>, ProblemHttpResult>> HandleAsync(
        LoginRequest request,
        ICommandHandler<LoginCommand, Result<UserId>> login,
        IQueryHandler<GetCurrentUserQuery, Result<CurrentUserResponse>> getCurrentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var loggedIn = await login.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (loggedIn.IsFailure)
        {
            return loggedIn.Error.ToProblem();
        }

        AntiforgeryTokens.Issue(httpContext);

        var currentUser = await getCurrentUser.HandleAsync(new GetCurrentUserQuery(loggedIn.Value), cancellationToken);
        return currentUser.ToHttpResult(TypedResults.Ok);
    }
}
