using HireLab.SharedKernel.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace HireLab.Api.Auth;

internal static class AntiforgerySetup
{
    public const string InvalidTokenCode = "Antiforgery.InvalidToken";

    private const string CookieName = "__Host-hirelab-antiforgery";

    public static IServiceCollection AddApiAntiforgery(this IServiceCollection services) =>
        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryTokens.HeaderName;
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.IsEssential = true;
        });

    public static TBuilder RequireAntiforgeryOnUnsafeMethods<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(ValidateAsync);

    private static async ValueTask<object?> ValidateAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (IsSafe(httpContext.Request.Method))
        {
            return await next(context);
        }

        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The antiforgery token is missing or invalid.",
                extensions: new Dictionary<string, object?> { [ErrorProblemDetails.CodeExtension] = InvalidTokenCode });
        }

        return await next(context);
    }

    private static bool IsSafe(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);
}
