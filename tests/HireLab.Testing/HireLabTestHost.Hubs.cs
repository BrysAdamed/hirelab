using HireLab.Api.Realtime;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Testing.Sessions;
using HireLab.Testing.Staff;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace HireLab.Testing;

public partial class HireLabTestHost
{
    /// <summary>Creates a connection to <c>/hubs/staff</c> that sends <paramref name="staff"/>'s staff cookie. Start it with <c>StartAsync</c>.</summary>
    public HubConnection CreateStaffHubConnection(SignedInStaff staff, HttpTransportType transport = HttpTransportType.WebSockets)
    {
        ArgumentNullException.ThrowIfNull(staff);
        return CreateHubConnection(StaffHub.Path, CookieHeader(staff.Cookies.Select(cookie => (cookie.Name.ToString(), cookie.Value.ToString()))), transport);
    }

    /// <summary>Creates a connection to <c>/hubs/candidate</c> that sends the candidate cookie <paramref name="candidate"/> holds.</summary>
    public HubConnection CreateCandidateHubConnection(BrowserSession candidate, HttpTransportType transport = HttpTransportType.WebSockets)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var cookie = candidate.Cookie(TestCandidateAccess.CookieName);
        return CreateHubConnection(
            CandidateHub.Path,
            cookie is null ? null : CookieHeader([(TestCandidateAccess.CookieName, cookie)]),
            transport);
    }

    /// <summary>How many candidate hub connections to a session are open.</summary>
    public int OpenCandidateConnections(Guid sessionId) =>
        Services.GetRequiredService<CandidateConnections>().PresenceOf(new InterviewSessionId(sessionId)).Connections;

    /// <summary>
    /// Waits until the candidate hub has registered <paramref name="count"/> open connections to a session. A started
    /// connection is registered just after its handshake, so a test that expects candidate events waits for this first.
    /// </summary>
    public async Task CandidateConnectionsOpenedAsync(Guid sessionId, int count)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Xunit.TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (OpenCandidateConnections(sessionId) < count)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private HubConnection CreateHubConnection(string path, string? cookieHeader, HttpTransportType transport)
    {
        var server = Factory.Server;
        var clientAddress = TestClientAddress.Next();

        return new HubConnectionBuilder()
            .WithUrl(new Uri(_baseAddress, path), options =>
            {
                options.Transports = transport;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Headers[TestClientAddress.HeaderName] = clientAddress;
                if (cookieHeader is not null)
                {
                    options.Headers[HeaderNames.Cookie] = cookieHeader;
                }

                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var client = server.CreateWebSocketClient();
                    client.ConfigureRequest = request =>
                    {
                        request.Headers[TestClientAddress.HeaderName] = clientAddress;
                        if (cookieHeader is not null)
                        {
                            request.Headers[HeaderNames.Cookie] = cookieHeader;
                        }
                    };
                    return await client.ConnectAsync(context.Uri, cancellationToken);
                };
            })
            .Build();
    }

    private static string CookieHeader(IEnumerable<(string Name, string Value)> cookies) =>
        string.Join("; ", cookies.DistinctBy(cookie => cookie.Name).Select(cookie => $"{cookie.Name}={cookie.Value}"));
}
