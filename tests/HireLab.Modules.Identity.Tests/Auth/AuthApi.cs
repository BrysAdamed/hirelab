using System.Net.Http.Json;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Tests.Auth;

internal static class AuthApi
{
    public static readonly Uri Login = new("/api/staff/auth/login", UriKind.Relative);
    public static readonly Uri Logout = new("/api/staff/auth/logout", UriKind.Relative);
    public static readonly Uri Me = new("/api/staff/auth/me", UriKind.Relative);

    public static Task<HttpResponseMessage> LoginAsync(this BrowserSession browser, string email, string password) =>
        browser.PostAsJsonAsync(Login, new { email, password });

    public static async Task<CurrentUser> CurrentUserAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<CurrentUser>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
}

internal sealed record CurrentUser(Guid Id, string Email, string DisplayName, StaffRole Role, CurrentUserTeam? Team);

internal sealed record CurrentUserTeam(Guid Id, string Name);
