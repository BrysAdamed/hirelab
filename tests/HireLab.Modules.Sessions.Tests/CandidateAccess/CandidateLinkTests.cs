using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Sessions;
using HireLab.Testing.TaskBank;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

public sealed class CandidateLinkTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Creating_the_link_returns_it_once_and_stores_only_the_token_hash()
    {
        // Arrange
        var (staff, sessionId, _, _) = await PlanSessionAsync();

        // Act
        using var response = await staff.PostAsync(TestCandidateAccess.CandidateLinkOf(sessionId.Value));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var issued = (await response.Content.ReadFromJsonAsync<IssuedLinkView>(CancellationToken)).ShouldNotBeNull();
        issued.LinkPath.ShouldMatch("^/s/[A-Za-z0-9_-]{43}$");
        issued.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
        var token = issued.LinkPath["/s/".Length..];
        var stored = await StoredLinkAsync(sessionId);
        stored.TokenHash.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token))));
        stored.TokenHash.ShouldNotContain(token);
        (await staff.SessionAsync(sessionId)).CandidateLinkCreatedAt.ShouldBe(Host.Time.GetUtcNow());
    }

    [Fact]
    public async Task A_session_has_no_link_until_one_is_created()
    {
        // Arrange
        var (staff, sessionId, _, _) = await PlanSessionAsync();

        // Act
        var session = await staff.SessionAsync(sessionId);

        // Assert
        session.CandidateLinkCreatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task The_link_can_be_created_while_the_session_is_live()
    {
        // Arrange
        var (staff, sessionId, teamId, _) = await PlanSessionAsync();
        await staff.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await staff.WentLiveAsync(sessionId);

        // Act
        var token = await staff.IssuedCandidateLinkAsync(sessionId.Value);

        // Assert
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(token);
    }

    [Fact]
    public async Task Regenerating_revokes_the_old_token_and_raises_the_revoked_event()
    {
        // Arrange
        var (staff, sessionId, _, _) = await PlanSessionAsync();
        var oldToken = await staff.IssuedCandidateLinkAsync(sessionId.Value);
        var oldLinkId = (await StoredLinkAsync(sessionId)).Id;
        Host.IntegrationEvents.Reset();

        // Act
        var newToken = await staff.IssuedCandidateLinkAsync(sessionId.Value);

        // Assert
        newToken.ShouldNotBe(oldToken);
        using var candidate = await Host.CreateCandidateBrowserAsync();
        using var revoked = await candidate.ExchangeAsync(oldToken);
        await revoked.ShouldBeProblemAsync(HttpStatusCode.NotFound, "CandidateLink.Invalid");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
        await candidate.ExchangedAsync(newToken);
        Host.IntegrationEvents.OfType<CandidateLinkRevoked>().ShouldBe([new CandidateLinkRevoked(sessionId, oldLinkId)]);
    }

    [Fact]
    public async Task Creating_the_first_link_raises_no_revoked_event()
    {
        // Arrange
        var (staff, sessionId, _, _) = await PlanSessionAsync();

        // Act
        await staff.IssuedCandidateLinkAsync(sessionId.Value);

        // Assert
        Host.IntegrationEvents.OfType<CandidateLinkRevoked>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Regenerating_invalidates_the_cookies_issued_from_the_old_link()
    {
        // Arrange
        var (staff, sessionId, _, _) = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(await staff.IssuedCandidateLinkAsync(sessionId.Value));

        // Act
        await staff.IssuedCandidateLinkAsync(sessionId.Value);

        // Assert
        using var response = await candidate.GetAsync(CandidateApi.Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemAsync()).Code().ShouldBe("CandidateLink.Invalid");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
    }

    [Fact]
    public async Task The_link_of_a_finished_session_can_not_be_regenerated()
    {
        // Arrange
        var (staff, sessionId, teamId, _) = await PlanSessionAsync();
        await staff.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await staff.WentLiveAsync(sessionId);
        await staff.FinishedAsync(sessionId);

        // Act
        using var response = await staff.PostAsync(TestCandidateAccess.CandidateLinkOf(sessionId.Value));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.Finished");
    }

    [Fact]
    public async Task The_link_can_not_be_regenerated_for_an_anonymised_candidate()
    {
        // Arrange
        var (staff, sessionId, _, candidateId) = await PlanSessionAsync();
        var token = await staff.IssuedCandidateLinkAsync(sessionId.Value);
        await Host.AnonymiseCandidateAsync(candidateId);

        // Act
        using var response = await staff.PostAsync(TestCandidateAccess.CandidateLinkOf(sessionId.Value));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.CandidateAnonymised");
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(token);
    }

    [Fact]
    public async Task The_link_can_not_be_created_for_a_closed_position()
    {
        // Arrange
        var (staff, sessionId, _, candidateId) = await PlanSessionAsync();
        await Host.ClosePositionOfAsync(candidateId);

        // Act
        using var response = await staff.PostAsync(TestCandidateAccess.CandidateLinkOf(sessionId.Value));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.PositionClosed");
        (await StoredLinkOrNullAsync(sessionId)).ShouldBeNull();
    }

    [Fact]
    public async Task Another_teams_session_gets_no_link()
    {
        // Arrange
        var (_, sessionId, _, _) = await PlanSessionAsync();
        using var otherTeam = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Interviewer));

        // Act
        using var response = await otherTeam.PostAsync(TestCandidateAccess.CandidateLinkOf(sessionId.Value));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
    }

    private async Task<(BrowserSession Staff, InterviewSessionId SessionId, TeamId TeamId, CandidateId CandidateId)> PlanSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var candidateId = await Host.CreateCandidateAsync(teamId);
        var created = await browser.CreatedAsync(candidateId);
        return (browser, created.SessionId, teamId, candidateId);
    }

    private async Task<Domain.CandidateLink> StoredLinkAsync(InterviewSessionId sessionId) =>
        (await StoredLinkOrNullAsync(sessionId)).ShouldNotBeNull();

    private async Task<Domain.CandidateLink?> StoredLinkOrNullAsync(InterviewSessionId sessionId)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SessionsDbContext>();
        var session = await dbContext.Sessions.IgnoreQueryFilters().AsNoTracking().SingleAsync(session => session.Id == sessionId, CancellationToken);
        return session.CandidateLink;
    }

    private sealed record IssuedLinkView(string LinkPath, DateTimeOffset CreatedAt);
}
