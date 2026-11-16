using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HireLab.Api.Auth;

/// <summary>
/// Checks the candidate cookie on every request with <see cref="ICandidateAccessValidator"/> (ADR 0008), so finishing a
/// session or regenerating its link cuts access at once. A rejected cookie is deleted, and the 401 carries the reason's
/// code, so the candidate app can tell "this interview has ended" from "this link doesn't work".
/// </summary>
internal static class CandidateCookieValidation
{
    private const string RejectionItem = ".hirelab.candidate_rejection";

    public static async Task OnValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        var httpContext = context.HttpContext;
        var validator = httpContext.RequestServices.GetRequiredService<ICandidateAccessValidator>();
        var access = await validator.ValidateAsync(context.Principal, httpContext.RequestAborted);
        if (access.IsSuccess)
        {
            return;
        }

        httpContext.Items[RejectionItem] = access.Error;
        context.RejectPrincipal();
        await httpContext.SignOutAsync(context.Scheme.Name);
    }

    public static async Task OnRedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var problem = new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Authentication is required." };
        if (httpContext.Items[RejectionItem] is Error rejection)
        {
            problem.Detail = rejection.Description;
            problem.Extensions[ErrorProblemDetails.CodeExtension] = rejection.Code;
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem });
    }
}
