using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Identity.Tests.Auth;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Tests.Invites;

internal static class InviteApi
{
    public const string ValidPassword = "invitee-password-not-a-secret";

    public static readonly Uri ManagerInvites = new("/api/staff/invites/managers", UriKind.Relative);
    public static readonly Uri InterviewerInvites = new("/api/staff/invites/interviewers", UriKind.Relative);
    public static readonly Uri TeamsWithoutManager = new("/api/staff/invites/managers/teams-without-manager", UriKind.Relative);
    public static readonly Uri Preview = new("/api/staff/invites/preview", UriKind.Relative);
    public static readonly Uri Redeem = new("/api/staff/invites/redeem", UriKind.Relative);

    public static Uri RevokeManagerInvite(Guid id) => new($"/api/staff/invites/managers/{id}/revoke", UriKind.Relative);

    public static Uri RevokeInterviewerInvite(Guid id) => new($"/api/staff/invites/interviewers/{id}/revoke", UriKind.Relative);

    public static Task<HttpResponseMessage> InviteManagerToNewTeamAsync(
        this BrowserSession browser,
        string email = "new.manager@example.com",
        string displayName = "Nora Newmanager",
        string teamName = "Fake Team Beta") =>
        browser.PostAsJsonAsync(ManagerInvites, new { email, displayName, newTeamName = teamName });

    public static Task<HttpResponseMessage> InviteManagerToTeamAsync(
        this BrowserSession browser,
        TeamId teamId,
        string email = "handover.manager@example.com",
        string displayName = "Hank Handover") =>
        browser.PostAsJsonAsync(ManagerInvites, new { email, displayName, teamId = teamId.Value });

    public static Task<HttpResponseMessage> InviteInterviewerAsync(
        this BrowserSession browser,
        string email = "new.interviewer@example.com",
        string displayName = "Ian Newinterviewer") =>
        browser.PostAsJsonAsync(InterviewerInvites, new { email, displayName });

    public static async Task<IssuedInviteView> IssuedAsync(this HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<IssuedInviteView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<IReadOnlyList<InviteView>> ListAsync(this BrowserSession browser, Uri uri)
    {
        using var response = await browser.GetAsync(uri);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<InviteView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> PreviewAsync(this BrowserSession browser, string token) =>
        browser.PostAsJsonAsync(Preview, new { token });

    public static Task<HttpResponseMessage> RedeemAsync(this BrowserSession browser, string token, string password = ValidPassword) =>
        browser.PostAsJsonAsync(Redeem, new { token, password });

    public static async Task ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.ShouldBe(status);
        (await response.ProblemAsync()).Code().ShouldBe(code);
    }
}

internal sealed record IssuedInviteView(InviteView Invite, string LinkPath)
{
    public string Token => LinkPath.Split('#')[1];
}

internal sealed record InviteView(
    Guid Id,
    string Email,
    string DisplayName,
    StaffRole Role,
    InviteTeam? Team,
    DateTimeOffset ExpiresAt,
    string State);

internal sealed record InviteTeam(Guid? Id, string Name, bool IsNew);

internal sealed record InvitePreview(string Email, string DisplayName, StaffRole Role, string? TeamName, bool IsNewTeam, DateTimeOffset ExpiresAt);

internal sealed record TeamWithoutManager(Guid Id, string Name);
