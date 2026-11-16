using HireLab.SharedKernel.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Auth.Antiforgery;

internal static class AntiforgeryTokenEndpoint
{
    public static RouteHandlerBuilder MapAntiforgeryToken(this IEndpointRouteBuilder auth) =>
        auth.MapGet("/antiforgery", Handle)
            .AllowAnonymous()
            .WithName("GetAntiforgeryToken")
            .WithSummary("Issues the XSRF-TOKEN cookie for the current user, signed in or not.");

    private static NoContent Handle(HttpContext httpContext)
    {
        AntiforgeryTokens.Issue(httpContext);
        return TypedResults.NoContent();
    }
}
