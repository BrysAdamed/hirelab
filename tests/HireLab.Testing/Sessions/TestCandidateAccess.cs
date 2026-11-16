using System.Net;
using System.Net.Http.Json;
using HireLab.Api.Auth;
using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using Microsoft.Net.Http.Headers;
using Shouldly;
using Xunit;

namespace HireLab.Testing.Sessions;

/// <summary>
/// Candidate links and candidate cookies through the real API (ADR 0008): staff plan a session and create its link, and
/// a candidate browser exchanges the link's token for the candidate cookie, as the candidate app does.
/// </summary>
public static class TestCandidateAccess
{
    /// <summary>The name of the candidate cookie.</summary>
    public const string CookieName = AuthCookies.Candidate;

    /// <summary>How many exchanges a client may send a minute before the rate limiter refuses them.</summary>
    public const int ExchangePermitLimit = RateLimitingSetup.AuthenticationPermitLimit;

    /// <summary>The anonymous endpoint that issues the candidate app's antiforgery token.</summary>
    public static readonly Uri AntiforgeryUri = new("/api/candidate/access/antiforgery", UriKind.Relative);

    /// <summary>The rate-limited endpoint that exchanges a link's token for the candidate cookie.</summary>
    public static readonly Uri ExchangeUri = new("/api/candidate/access", UriKind.Relative);

    /// <summary>The staff endpoint that creates or regenerates a session's candidate link.</summary>
    public static Uri CandidateLinkOf(Guid sessionId) => new($"/api/staff/sessions/{sessionId}/candidate-link", UriKind.Relative);

    /// <summary>Creates a browser on the candidate app with an anonymous antiforgery token, as the app has before it exchanges a link.</summary>
    public static async Task<BrowserSession> CreateCandidateBrowserAsync(this HireLabTestHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var browser = host.CreateBrowser();
        using var response = await browser.GetAsync(AntiforgeryUri);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        return browser;
    }

    /// <summary>Sends a link's token to the exchange endpoint.</summary>
    public static Task<HttpResponseMessage> ExchangeAsync(this BrowserSession candidate, string token)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.PostAsJsonAsync(ExchangeUri, new { token });
    }

    /// <summary>Sends a link's token to the exchange endpoint and asserts that it gave the candidate cookie.</summary>
    public static async Task ExchangedAsync(this BrowserSession candidate, string token)
    {
        using var response = await candidate.ExchangeAsync(token);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        candidate.Cookie(AuthCookies.Candidate).ShouldNotBeNull();
    }

    /// <summary>Creates or regenerates a session's candidate link as <paramref name="staff"/> and returns the link's token.</summary>
    public static async Task<string> IssuedCandidateLinkAsync(this BrowserSession staff, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(staff);

        using var response = await staff.PostAsync(CandidateLinkOf(sessionId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var issued = (await response.Content.ReadFromJsonAsync<IssuedCandidateLink>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        issued.LinkPath.ShouldStartWith("/s/");
        return issued.LinkPath["/s/".Length..];
    }

    /// <summary>
    /// Plans a session for a new candidate of a new team, creates its candidate link and exchanges it; returns the
    /// candidate cookie (one per candidate path).
    /// </summary>
    internal static async Task<IReadOnlyList<SetCookieHeaderValue>> SignInAsCandidateAsync(HireLabTestHost host)
    {
        var interviewer = await host.SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await host.CreateCandidateAsync(interviewer.User.TeamId!.Value);

        using var staff = await host.CreateBrowserAsync(interviewer.Cookies);
        var sessionId = await staff.PlannedSessionAsync(candidateId);
        var token = await staff.IssuedCandidateLinkAsync(sessionId);

        using var candidate = await host.CreateCandidateBrowserAsync();
        using var response = await candidate.ExchangeAsync(token);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return SetCookieHeaderValue.ParseList(response.Headers.GetValues(HeaderNames.SetCookie).ToList())
            .Where(cookie => cookie.Name == AuthCookies.Candidate)
            .ToList();
    }

    private static async Task<Guid> PlannedSessionAsync(this BrowserSession staff, CandidateId candidateId)
    {
        using var response = await staff.PostAsJsonAsync(
            new Uri($"/api/staff/candidates/{candidateId}/sessions", UriKind.Relative),
            new { scheduledAt = (DateTimeOffset?)null });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PlannedSession>(TestContext.Current.CancellationToken)).ShouldNotBeNull().Id;
    }

    private sealed record IssuedCandidateLink(string LinkPath, DateTimeOffset CreatedAt);

    private sealed record PlannedSession(Guid Id);
}
