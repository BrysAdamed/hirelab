using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.SharedKernel.Http;

/// <summary>
/// The CSRF contract between the API and the SPAs (ADR 0007): the API sends a request token in the readable
/// <see cref="CookieName"/> cookie, and the SPA copies it into the <see cref="HeaderName"/> header on every unsafe method.
/// The token is bound to the request's user, so an endpoint that changes the user (login, logout) issues a new one.
/// </summary>
public static class AntiforgeryTokens
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";

    /// <summary>Issues a request token for the user in <see cref="HttpContext.User"/> as the readable cookie.</summary>
    public static void Issue(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);

        context.Response.Cookies.Append(CookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        });
    }
}
