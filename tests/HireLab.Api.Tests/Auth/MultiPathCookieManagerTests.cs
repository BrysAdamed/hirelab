using HireLab.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HireLab.Api.Tests.Auth;

public sealed class MultiPathCookieManagerTests(ApiTestHost host) : IntegrationTest(host)
{
    private const string CookieName = "__Secure-example";
    private static readonly string[] _paths = ["/api/example", "/hubs/example"];

    [Fact]
    public void Append_writes_the_cookie_once_per_path()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var manager = new MultiPathCookieManager(_paths);

        // Act
        manager.AppendResponseCookie(context, CookieName, "fake-value", new CookieOptions { Path = "/", Secure = true, HttpOnly = true });

        // Assert
        var cookies = SetCookiesOf(context);
        cookies.Select(cookie => cookie.Path.Value).ShouldBe(_paths);
        cookies.ShouldAllBe(cookie => cookie.Name == CookieName && cookie.Value == "fake-value" && cookie.Secure && cookie.HttpOnly);
    }

    [Fact]
    public void Delete_expires_the_cookie_on_every_path()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var manager = new MultiPathCookieManager(_paths);

        // Act
        manager.DeleteCookie(context, CookieName, new CookieOptions { Path = "/" });

        // Assert
        var cookies = SetCookiesOf(context);
        cookies.Select(cookie => cookie.Path.Value).ShouldBe(_paths);
        cookies.ShouldAllBe(cookie => cookie.Name == CookieName && cookie.Expires < DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Candidate_sign_in_writes_the_cookie_for_the_candidate_api_and_hub_paths()
    {
        // Act
        var cookies = await Host.SignInAsCandidateAsync();

        // Assert
        cookies.Select(cookie => cookie.Path.Value).ShouldBe(AuthCookies.CandidatePaths, ignoreOrder: true);
        cookies.ShouldAllBe(cookie => cookie.Name == AuthCookies.Candidate
            && cookie.Secure
            && cookie.HttpOnly
            && cookie.SameSite == Microsoft.Net.Http.Headers.SameSiteMode.Strict);
    }

    private static IList<SetCookieHeaderValue> SetCookiesOf(HttpContext context) =>
        SetCookieHeaderValue.ParseList(context.Response.Headers.SetCookie.OfType<string>().ToList());
}
