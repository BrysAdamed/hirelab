using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.SharedKernel.Http;

/// <summary>
/// The CSRF contract between the API and the SPAs (ADR 0007): the API sends a request token in a readable cookie, and
/// the SPA copies it into the <see cref="HeaderName"/> header on every unsafe method. The token is bound to the
/// request's user, so an endpoint that changes the user (login, logout, the candidate link exchange) issues a new one.
/// Staff get <see cref="CookieName"/> and candidates <see cref="CandidateCookieName"/>, so the two apps opened in one
/// browser don't overwrite each other's token (ADR 0031).
/// </summary>
public static class AntiforgeryTokens
{
    public const string CookieName = "XSRF-TOKEN";
    public const string CandidateCookieName = "XSRF-TOKEN-CANDIDATE";
    public const string HeaderName = "X-XSRF-TOKEN";

    /// <summary>Issues a request token for the staff user in <see cref="HttpContext.User"/> as <see cref="CookieName"/>.</summary>
    public static void Issue(HttpContext context) => Issue(context, CookieName);

    /// <summary>Issues a request token for the candidate in <see cref="HttpContext.User"/> as <see cref="CandidateCookieName"/>.</summary>
    public static void IssueForCandidate(HttpContext context) => Issue(context, CandidateCookieName);

    private static void Issue(HttpContext context, string cookieName)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);

        context.Response.Cookies.Append(cookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        });
    }
}
