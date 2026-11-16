using System.Net.Http.Json;
using HireLab.SharedKernel.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace HireLab.Testing;

/// <summary>
/// An API client that behaves like an SPA in a browser: it keeps the cookies the API sets and copies the
/// <c>XSRF-TOKEN</c> cookie into the <c>X-XSRF-TOKEN</c> header on unsafe methods. Create it with
/// <see cref="HireLabTestHost.CreateBrowserAsync"/>. Each session has its own client IP address.
/// </summary>
public sealed class BrowserSession : IDisposable
{
    /// <summary>The endpoint that issues an antiforgery token for the current user.</summary>
    public static readonly Uri StaffAntiforgeryUri = new("/api/staff/auth/antiforgery", UriKind.Relative);

    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    internal BrowserSession(HttpClient client, IEnumerable<SetCookieHeaderValue> cookies)
    {
        _client = client;
        Store(cookies);
    }

    /// <summary>Whether unsafe requests carry the <c>X-XSRF-TOKEN</c> header. Turn it off to test the antiforgery check.</summary>
    public bool SendsAntiforgeryHeader { get; set; } = true;

    /// <summary>The current value of a cookie, or <c>null</c> when the browser has none.</summary>
    public string? Cookie(string name) => _cookies.GetValueOrDefault(name);

    /// <summary>Gets a new antiforgery token for the current user, as the staff app does before it logs in.</summary>
    public async Task RefreshAntiforgeryTokenAsync()
    {
        using var response = await GetAsync(StaffAntiforgeryUri);
        response.EnsureSuccessStatusCode();
    }

    public Task<HttpResponseMessage> GetAsync(Uri uri) => SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));

    public Task<HttpResponseMessage> PostAsync(Uri uri) => SendAsync(new HttpRequestMessage(HttpMethod.Post, uri));

    public Task<HttpResponseMessage> PostAsJsonAsync<TBody>(Uri uri, TBody body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> PutAsJsonAsync<TBody>(Uri uri, TBody body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Put, uri) { Content = JsonContent.Create(body) });

    public Task<HttpResponseMessage> DeleteAsync(Uri uri) => SendAsync(new HttpRequestMessage(HttpMethod.Delete, uri));

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using (request)
        {
            if (_cookies.Count > 0)
            {
                request.Headers.Add(HeaderNames.Cookie, string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
            }

            if (SendsAntiforgeryHeader && !IsSafe(request.Method) && Cookie(AntiforgeryTokens.CookieName) is { } token)
            {
                request.Headers.Add(AntiforgeryTokens.HeaderName, token);
            }

            var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            if (response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookies))
            {
                Store(SetCookieHeaderValue.ParseList(setCookies.ToList()));
            }

            return response;
        }
    }

    public void Dispose() => _client.Dispose();

    private void Store(IEnumerable<SetCookieHeaderValue> cookies)
    {
        foreach (var cookie in cookies)
        {
            var name = cookie.Name.ToString();
            if (cookie.Value.Length == 0 || cookie.Expires is { Year: <= 1970 })
            {
                _cookies.Remove(name);
            }
            else
            {
                _cookies[name] = cookie.Value.ToString();
            }
        }
    }

    private static bool IsSafe(HttpMethod method) =>
        method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options || method == HttpMethod.Trace;
}
