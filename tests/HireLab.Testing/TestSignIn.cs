using System.Security.Claims;
using HireLab.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace HireLab.Testing;

internal sealed class TestSignIn : IStartupFilter
{
    public const string Staff = AuthSchemes.Staff;
    public const string Candidate = AuthSchemes.Candidate;

    private const string PathPrefix = "/__test/sign-in";

    public static Uri UriFor(string scheme) => new($"{PathPrefix}/{scheme}", UriKind.Relative);

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Map(PathPrefix, branch => branch.Run(SignInAsync));
        next(app);
    };

    private static async Task SignInAsync(HttpContext context)
    {
        var scheme = context.Request.Path.Value?.Trim('/');
        if (scheme is not (Staff or Candidate))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())], scheme);
        await context.SignInAsync(scheme, new ClaimsPrincipal(identity));
    }
}
