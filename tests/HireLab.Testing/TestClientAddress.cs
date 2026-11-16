using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace HireLab.Testing;

/// <summary>
/// Gives every test client its own remote IP address, so per-IP rate limits don't leak between tests. The test server
/// has no real connection, so the address travels in a test-only header.
/// </summary>
internal sealed class TestClientAddress : IStartupFilter
{
    public const string HeaderName = "X-Test-Client-Address";

    private static int _sequence;

    public static string Next()
    {
        var number = Interlocked.Increment(ref _sequence);
        return new IPAddress([10, (byte)(number >> 16), (byte)(number >> 8), (byte)number]).ToString();
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            if (IPAddress.TryParse(context.Request.Headers[HeaderName], out var address))
            {
                context.Connection.RemoteIpAddress = address;
            }

            return nextMiddleware(context);
        });
        next(app);
    };
}
