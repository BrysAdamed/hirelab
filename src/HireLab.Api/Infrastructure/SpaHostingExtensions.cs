using System.Text.RegularExpressions;
using HireLab.ServiceDefaults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace HireLab.Api.Infrastructure;

internal static partial class SpaHostingExtensions
{
    private const string StaffAppDirectory = "staff";
    private const string CandidateAppDirectory = "candidate";
    private const string CandidateBasePath = "/s";
    private const string IndexFile = "index.html";
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    private const string NoCacheControl = "no-cache";

    private static readonly string[] _serverPathPrefixes =
    [
        "/api",
        "/hubs",
        "/openapi",
        ServiceDefaultsExtensions.AlivenessEndpointPath,
        ServiceDefaultsExtensions.HealthEndpointPath,
    ];

    public static WebApplication MapSpaHosting(this WebApplication app)
    {
        foreach (var prefix in _serverPathPrefixes)
        {
            app.MapFallback($"{prefix}/{{**path}}", () => Results.NotFound());
        }

        var webRootPath = app.Environment.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            return app;
        }

        MapSpa(app, Path.Combine(webRootPath, CandidateAppDirectory), CandidateBasePath, "s/{*path:nonfile}");
        MapSpa(app, Path.Combine(webRootPath, StaffAppDirectory), string.Empty, "{*path:nonfile}");

        return app;
    }

    private static void MapSpa(WebApplication app, string rootPath, string requestPath, string fallbackPattern)
    {
        if (!File.Exists(Path.Combine(rootPath, IndexFile)))
        {
            return;
        }

        var fileProvider = new PhysicalFileProvider(rootPath);

        app.UseStaticFiles(CreateStaticFileOptions(fileProvider, requestPath));
        app.MapFallbackToFile(fallbackPattern, IndexFile, CreateStaticFileOptions(fileProvider, string.Empty));
    }

    private static StaticFileOptions CreateStaticFileOptions(IFileProvider fileProvider, string requestPath) => new()
    {
        FileProvider = fileProvider,
        RequestPath = requestPath,
        OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] =
            IsFingerprinted(context.File.Name) ? ImmutableCacheControl : NoCacheControl,
    };

    private static bool IsFingerprinted(string fileName) => FingerprintedFileName().IsMatch(fileName);

    [GeneratedRegex(@"-[A-Z0-9]{8}\.[a-z0-9]+$")]
    private static partial Regex FingerprintedFileName();
}
