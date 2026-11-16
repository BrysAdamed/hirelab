using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HireLab.Api.Auth;

internal static class AuthenticationSetup
{
    private static readonly TimeSpan _candidateLifetime = TimeSpan.FromHours(12);

    public static IServiceCollection AddStaffAndCandidateAuth(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddCookie(AuthSchemes.Staff, options =>
            {
                ConfigureApiCookie(options, AuthCookies.Staff);
                options.ExpireTimeSpan = StaffSessionLifetime.IdleTimeout;
                options.SlidingExpiration = true;
                options.Events.OnSigningIn = StaffSessionLifetime.OnSigningInAsync;
                options.Events.OnValidatePrincipal = StaffSessionLifetime.OnValidatePrincipalAsync;
            })
            .AddCookie(AuthSchemes.Candidate, options =>
            {
                ConfigureApiCookie(options, AuthCookies.Candidate);
                options.CookieManager = new MultiPathCookieManager(AuthCookies.CandidatePaths);
                options.ExpireTimeSpan = _candidateLifetime;
                options.SlidingExpiration = false;
                options.Events.OnValidatePrincipal = CandidateCookieValidation.OnValidatePrincipalAsync;
                options.Events.OnRedirectToLogin = CandidateCookieValidation.OnRedirectToLoginAsync;
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Staff, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Staff)
                .RequireAuthenticatedUser())
            .AddPolicy(AuthPolicies.Candidate, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Candidate)
                .RequireAuthenticatedUser())
            .AddStaffPolicies();

        return services;
    }

    private static void ConfigureApiCookie(CookieAuthenticationOptions options, string cookieName)
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;

        options.Events.OnRedirectToLogin = context =>
            WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Authentication is required.");
        options.Events.OnRedirectToAccessDenied = context =>
            WriteProblemAsync(context, StatusCodes.Status403Forbidden, "The operation is not allowed.");
    }

    private static async Task WriteProblemAsync(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode,
        string title)
    {
        context.Response.StatusCode = statusCode;

        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title },
        });
    }
}
