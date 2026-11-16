using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth.Logout;

internal static class LogoutEndpoint
{
    public static RouteHandlerBuilder MapLogout(this IEndpointRouteBuilder auth) =>
        auth.MapPost("/logout", HandleAsync)
            .WithName("Logout")
            .WithSummary("Ends the staff session and issues an XSRF-TOKEN for the anonymous user.");

    private static async Task<NoContent> HandleAsync(SignInManager<User> signInManager, HttpContext httpContext)
    {
        await signInManager.SignOutAsync();
        AntiforgeryTokens.Issue(httpContext);
        return TypedResults.NoContent();
    }
}
