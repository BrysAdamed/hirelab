using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace HireLab.ServiceDefaults;

/// <summary>
/// Cross-cutting defaults shared by every HireLab service: OpenTelemetry, health checks,
/// service discovery and HTTP client resilience.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>Liveness endpoint: the process is up and responding.</summary>
    public const string AlivenessEndpointPath = "/alive";

    /// <summary>Readiness endpoint: the service and its dependencies (such as the database) are healthy.</summary>
    public const string HealthEndpointPath = "/health";

    /// <summary>Health checks with this tag also run on the liveness endpoint.</summary>
    public const string LiveTag = "live";

    private static readonly string[] _requestLoggingCategories =
    [
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "System.Net.Http.HttpClient",
    ];

    /// <summary>
    /// Adds OpenTelemetry, the default health checks, service discovery and resilient HTTP clients.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures logs, metrics and traces exported over OTLP. Request bodies and query strings are never recorded.
    /// </summary>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        foreach (var category in _requestLoggingCategories)
        {
            builder.Logging.AddFilter(category, LogLevel.Warning);
        }

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options => options.Filter = context => !IsHealthEndpoint(context.Request.Path))
                .AddHttpClientInstrumentation()
                .AddProcessor(new QueryStringRedactionProcessor()));

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// Adds the always-healthy liveness check. Readiness checks (such as the database) are added by the integrations.
    /// </summary>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);

        return builder;
    }

    /// <summary>
    /// Maps <c>/alive</c> and <c>/health</c> in every environment. Both return only the overall status.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath).AllowAnonymous();
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(LiveTag),
            })
            .AllowAnonymous();

        return app;
    }

    private static bool IsHealthEndpoint(PathString path) =>
        path.StartsWithSegments(HealthEndpointPath) || path.StartsWithSegments(AlivenessEndpointPath);
}
