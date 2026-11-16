using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Identity.Tests.Users;

internal static class UserApi
{
    public static readonly Uri Users = new("/api/staff/users", UriKind.Relative);
    public static readonly Uri MyTeamInterviewers = new("/api/staff/my-team/interviewers", UriKind.Relative);
    public static readonly Uri Teams = new("/api/staff/teams", UriKind.Relative);

    public static Uri Deactivate(UserId userId) => new($"/api/staff/users/{userId}/deactivate", UriKind.Relative);

    public static Uri Reactivate(UserId userId) => new($"/api/staff/users/{userId}/reactivate", UriKind.Relative);

    public static Uri Reassign(UserId userId) => new($"/api/staff/users/{userId}/reassign", UriKind.Relative);

    public static Uri DeactivateInterviewer(UserId userId) => new($"/api/staff/my-team/interviewers/{userId}/deactivate", UriKind.Relative);

    public static Uri ReactivateInterviewer(UserId userId) => new($"/api/staff/my-team/interviewers/{userId}/reactivate", UriKind.Relative);

    public static Uri RenameTeam(TeamId teamId) => new($"/api/staff/teams/{teamId}/rename", UriKind.Relative);

    public static Task<HttpResponseMessage> ReassignAsync(this BrowserSession browser, UserId userId, TeamId teamId) =>
        browser.PostAsJsonAsync(Reassign(userId), new { teamId = teamId.Value });

    public static Task<HttpResponseMessage> RenameTeamAsync(this BrowserSession browser, TeamId teamId, string name) =>
        browser.PostAsJsonAsync(RenameTeam(teamId), new { name });

    public static async Task<List<UserView>> UsersAsync(this BrowserSession browser, Uri uri)
    {
        using var response = await browser.GetAsync(uri);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<UserView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<StoredUser> StoredUserAsync(this HireLabTestHost host, UserId userId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        return await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new StoredUser(user.IsActive, user.TeamId, user.SecurityStamp))
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<List<TeamView>> TeamsAsync(this BrowserSession browser)
    {
        using var response = await browser.GetAsync(Teams);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<TeamView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

internal sealed record StoredUser(bool IsActive, TeamId? TeamId, string? SecurityStamp);

internal sealed record UserView(Guid Id, string Email, string DisplayName, StaffRole Role, UserTeamView? Team, bool IsActive);

internal sealed record UserTeamView(Guid Id, string Name);

internal sealed record TeamView(Guid Id, string Name, UserView? Manager, List<UserView> Members);
