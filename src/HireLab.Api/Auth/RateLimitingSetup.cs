using System.Globalization;
using System.Threading.RateLimiting;
using HireLab.SharedKernel.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace HireLab.Api.Auth;

internal static class RateLimitingSetup
{
    public const int AuthenticationPermitLimit = 10;

    /// <summary>
    /// Overrides <see cref="AuthenticationPermitLimit"/>. Only the E2E suite sets it: all its browsers reach the API
    /// through the dev server proxy from one address.
    /// </summary>
    public const string AuthenticationPermitLimitKey = "RateLimiting:AuthenticationPermitLimit";

    public static readonly TimeSpan AuthenticationWindow = TimeSpan.FromMinutes(1);

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue(AuthenticationPermitLimitKey, AuthenticationPermitLimit);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteProblemAsync;
            options.AddPolicy(RateLimitPolicies.Authentication, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = AuthenticationWindow,
                    QueueLimit = 0,
                }));
        });
    }

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many attempts. Try again in a minute.",
            },
        });
    }
}
