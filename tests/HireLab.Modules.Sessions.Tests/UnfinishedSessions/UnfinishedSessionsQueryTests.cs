using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Sessions.Tests.UnfinishedSessions;

public sealed class UnfinishedSessionsQueryTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData("Planned", true)]
    [InlineData("Live", true)]
    [InlineData("Finished", false)]
    public async Task A_session_is_unfinished_until_it_is_finished(string state, bool unfinished)
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(candidateId);
        await Host.SetStateAsync(created.SessionId, Enum.Parse<InterviewSessionState>(state));

        // Act
        var any = await AnyAsAsync(staff.User, candidateId);

        // Assert
        any.ShouldBe(unfinished);
    }

    [Fact]
    public async Task Only_the_given_candidates_sessions_count()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var teamId = staff.User.TeamId!.Value;
        var interviewed = await Host.CreateCandidateAsync(teamId);
        var finished = await Host.CreateCandidateAsync(teamId);
        var withoutSession = await Host.CreateCandidateAsync(teamId);
        using var browser = await CreateBrowserAsync(staff);
        await browser.CreatedAsync(interviewed);
        await Host.SetStateAsync((await browser.CreatedAsync(finished)).SessionId, InterviewSessionState.Finished);

        // Act
        var none = await AnyAsAsync(staff.User, finished, withoutSession);
        var some = await AnyAsAsync(staff.User, withoutSession, interviewed);
        var empty = await AnyAsAsync(staff.User);

        // Assert
        none.ShouldBeFalse();
        some.ShouldBeTrue();
        empty.ShouldBeFalse();
    }

    [Fact]
    public async Task Closing_a_position_is_refused_while_a_real_session_is_unfinished()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        await browser.CreatedAsync(candidateId);
        using var candidate = await browser.GetAsync(new Uri($"/api/staff/candidates/{candidateId}", UriKind.Relative));
        var positionId = (await candidate.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("positionId").GetGuid();
        using var position = await browser.GetAsync(new Uri($"/api/staff/positions/{positionId}", UriKind.Relative));
        var version = (await position.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("version").GetUInt32();

        // Act
        using var close = await browser.PostAsJsonAsync(new Uri($"/api/staff/positions/{positionId}/close", UriKind.Relative), new { version });

        // Assert
        await close.ShouldBeProblemAsync(HttpStatusCode.Conflict, "JobPosition.UnfinishedSessions");
    }

    [Fact]
    public async Task Another_teams_and_the_admins_requests_see_no_unfinished_session()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        await browser.CreatedAsync(candidateId);
        var otherManager = await Host.CreateUserAsync(UserBuilder.Manager((await Host.CreateTeamAsync()).Id));
        var admin = await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var otherTeam = await AnyAsAsync(otherManager, candidateId);
        var asAdmin = await AnyAsAsync(admin, candidateId);

        // Assert
        otherTeam.ShouldBeFalse();
        asAdmin.ShouldBeFalse();
    }

    private async Task<bool> AnyAsAsync(TestUser user, params CandidateId[] candidateIds)
    {
        await using var scope = Host.CreateScopeAs(user);
        return await scope.Services.GetRequiredService<IUnfinishedSessionsQuery>().AnyForCandidatesAsync(candidateIds, CancellationToken);
    }
}
