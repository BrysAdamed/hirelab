using System.Net;
using System.Net.Http.Json;

namespace HireLab.Modules.Identity.Tests.ResetLinks;

internal static class ResetLinkApi
{
    public const string NewPassword = "reset-password-not-a-secret";

    public static readonly Uri Preview = new("/api/staff/reset-links/preview", UriKind.Relative);
    public static readonly Uri Redeem = new("/api/staff/reset-links/redeem", UriKind.Relative);
    public static readonly Uri ChangePassword = new("/api/staff/auth/change-password", UriKind.Relative);

    public static Uri ForManager(Guid userId) => new($"/api/staff/reset-links/managers/{userId}", UriKind.Relative);

    public static Uri ForInterviewer(Guid userId) => new($"/api/staff/reset-links/interviewers/{userId}", UriKind.Relative);

    public static Task<HttpResponseMessage> GenerateForManagerAsync(this BrowserSession browser, Guid userId) =>
        browser.PostAsync(ForManager(userId));

    public static Task<HttpResponseMessage> GenerateForInterviewerAsync(this BrowserSession browser, Guid userId) =>
        browser.PostAsync(ForInterviewer(userId));

    public static async Task<IssuedResetLinkView> IssuedAsync(this HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<IssuedResetLinkView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> PreviewResetLinkAsync(this BrowserSession browser, string token) =>
        browser.PostAsJsonAsync(Preview, new { token });

    public static Task<HttpResponseMessage> RedeemResetLinkAsync(this BrowserSession browser, string token, string password = NewPassword) =>
        browser.PostAsJsonAsync(Redeem, new { token, password });

    public static Task<HttpResponseMessage> ChangePasswordAsync(this BrowserSession browser, string currentPassword, string newPassword) =>
        browser.PostAsJsonAsync(ChangePassword, new { currentPassword, newPassword });
}

internal sealed record IssuedResetLinkView(Guid UserId, DateTimeOffset ExpiresAt, string LinkPath)
{
    public string Token => LinkPath.Split('#')[1];
}

internal sealed record ResetLinkPreview(string Email, string DisplayName, DateTimeOffset ExpiresAt);
