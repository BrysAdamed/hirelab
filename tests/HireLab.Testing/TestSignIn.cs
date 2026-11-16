using System.Security.Claims;
using HireLab.Api.Auth;
using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Testing;

internal sealed class TestSignIn : IStartupFilter
{
    private const string PathPrefix = "/__test/sign-in";

    public static Uri CandidateUri { get; } = new($"{PathPrefix}/{AuthSchemes.Candidate}", UriKind.Relative);

    public static Uri StaffUri(UserId userId) => new($"{PathPrefix}/{AuthSchemes.Staff}/{userId}", UriKind.Relative);

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Map(PathPrefix, branch => branch.Run(SignInAsync));
        next(app);
    };

    private static Task SignInAsync(HttpContext context) =>
        context.Request.Path.Value?.Trim('/').Split('/') switch
        {
            [AuthSchemes.Candidate] => SignInCandidateAsync(context),
            [AuthSchemes.Staff, var userId] => SignInStaffAsync(context, userId),
            _ => NotFoundAsync(context),
        };

    private static Task SignInCandidateAsync(HttpContext context)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())], AuthSchemes.Candidate);
        return context.SignInAsync(AuthSchemes.Candidate, new ClaimsPrincipal(identity));
    }

    private static async Task SignInStaffAsync(HttpContext context, string userId)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByIdAsync(userId) is not { } user)
        {
            await NotFoundAsync(context);
            return;
        }

        await context.RequestServices.GetRequiredService<SignInManager<User>>().SignInAsync(user, isPersistent: false);
    }

    private static Task NotFoundAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}
