using System.Net;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.TaskBank;

namespace HireLab.Modules.Sessions.Tests.Sessions;

public sealed class SessionTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task A_team_member_plans_sessions_numbered_per_candidate(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value, "Jane Fakeperson");
        var otherCandidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        var first = await browser.CreatedAsync(candidateId);
        var second = await browser.CreatedAsync(candidateId);
        var otherFirst = await browser.CreatedAsync(otherCandidateId);

        // Assert
        first.Number.ShouldBe(1);
        second.Number.ShouldBe(2);
        otherFirst.Number.ShouldBe(1);
        var session = await browser.SessionAsync(first.SessionId);
        session.State.ShouldBe(InterviewSessionState.Planned);
        session.CandidateId.ShouldBe(candidateId.Value);
        session.CandidateDisplayName.ShouldBe("Jane Fakeperson");
        session.CreatedById.ShouldBe(staff.User.Id.Value);
        session.CreatedByName.ShouldBe(staff.User.DisplayName);
        session.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
        session.ScheduledAt.ShouldBeNull();
        session.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_session_can_be_scheduled_and_rescheduled_while_planned()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        var scheduledAt = new DateTimeOffset(2026, 2, 3, 10, 30, 0, TimeSpan.Zero);
        var created = await browser.CreatedAsync(candidateId, scheduledAt);

        // Act
        var scheduled = await browser.SessionAsync(created.SessionId);
        using var cleared = await browser.PutAsJsonAsync(SessionApi.Schedule(created.SessionId), new { scheduledAt = (DateTimeOffset?)null });

        // Assert
        scheduled.ScheduledAt.ShouldBe(scheduledAt);
        cleared.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await browser.SessionAsync(created.SessionId)).ScheduledAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_started_session_can_not_be_rescheduled()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        using var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(staff.User.TeamId!.Value));
        await Host.SetStateAsync(created.SessionId, InterviewSessionState.Live);

        // Act
        using var response = await browser.PutAsJsonAsync(SessionApi.Schedule(created.SessionId), new { scheduledAt = Host.Time.GetUtcNow() });

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotPlanned");
    }

    [Fact]
    public async Task A_session_is_refused_for_an_anonymised_candidate()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        await Host.AnonymiseCandidateAsync(candidateId);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.CreateAsync(candidateId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.CandidateAnonymised");
    }

    [Fact]
    public async Task A_session_is_refused_for_a_candidate_of_a_closed_position()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        await Host.ClosePositionOfAsync(candidateId);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var response = await browser.CreateAsync(candidateId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.PositionClosed");
    }

    [Fact]
    public async Task Another_teams_candidate_and_session_are_not_found()
    {
        // Arrange
        var other = await SignInAsAsync(StaffRole.Interviewer);
        var otherCandidateId = await Host.CreateCandidateAsync(other.User.TeamId!.Value);
        using var otherBrowser = await CreateBrowserAsync(other);
        var otherSession = await otherBrowser.CreatedAsync(otherCandidateId);
        var staff = await SignInAsAsync(StaffRole.Manager);
        using var browser = await CreateBrowserAsync(staff);

        // Act
        using var create = await browser.CreateAsync(otherCandidateId);
        using var list = await browser.GetAsync(SessionApi.SessionsOf(otherCandidateId));
        using var get = await browser.GetAsync(SessionApi.Session(otherSession.SessionId));
        using var delete = await browser.DeleteAsync(SessionApi.Session(otherSession.SessionId));

        // Assert
        await create.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await list.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Candidate.NotFound");
        await get.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await delete.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        (await browser.TeamSessionsAsync()).ShouldBeEmpty();
        (await otherBrowser.SessionAsync(otherSession.SessionId)).Number.ShouldBe(1);
    }

    [Fact]
    public async Task A_candidates_sessions_are_listed_by_number()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        var first = await browser.CreatedAsync(candidateId);
        var second = await browser.CreatedAsync(candidateId);
        await Host.SetStateAsync(first.SessionId, InterviewSessionState.Finished);
        await browser.CreatedAsync(await Host.CreateCandidateAsync(staff.User.TeamId!.Value));

        // Act
        var sessions = await browser.CandidateSessionsAsync(candidateId);

        // Assert
        sessions.Select(session => (session.Id, session.Number, session.State)).ShouldBe(
        [
            (first.Id, 1, InterviewSessionState.Finished),
            (second.Id, 2, InterviewSessionState.Planned),
        ]);
    }

    [Fact]
    public async Task The_team_sessions_are_the_unfinished_ones_soonest_first()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Manager);
        var teamId = staff.User.TeamId!.Value;
        using var browser = await CreateBrowserAsync(staff);
        var candidateId = await Host.CreateCandidateAsync(teamId, "Jane Fakeperson");
        var unscheduled = await browser.CreatedAsync(candidateId);
        var later = await browser.CreatedAsync(candidateId, Host.Time.GetUtcNow().AddDays(3));
        var sooner = await browser.CreatedAsync(candidateId, Host.Time.GetUtcNow().AddDays(1));
        var live = await browser.CreatedAsync(candidateId, Host.Time.GetUtcNow().AddDays(5));
        var finished = await browser.CreatedAsync(candidateId, Host.Time.GetUtcNow().AddHours(1));
        await Host.SetStateAsync(live.SessionId, InterviewSessionState.Live);
        await Host.SetStateAsync(finished.SessionId, InterviewSessionState.Finished);

        // Act
        var sessions = await browser.TeamSessionsAsync();

        // Assert
        sessions.Select(session => session.Id).ShouldBe([live.Id, sooner.Id, later.Id, unscheduled.Id]);
        sessions[0].State.ShouldBe(InterviewSessionState.Live);
        sessions.ShouldAllBe(session => session.CandidateDisplayName == "Jane Fakeperson" && session.CandidateId == candidateId.Value);
    }

    [Fact]
    public async Task The_team_sessions_show_an_anonymised_candidate_by_number()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var candidateId = await Host.CreateCandidateAsync(staff.User.TeamId!.Value);
        using var browser = await CreateBrowserAsync(staff);
        await browser.CreatedAsync(candidateId);
        await Host.AnonymiseCandidateAsync(candidateId);

        // Act
        var sessions = await browser.TeamSessionsAsync();

        // Assert
        sessions.Single().CandidateDisplayName.ShouldBe("Candidate #1");
    }

    [Fact]
    public async Task A_planned_session_is_deleted_with_its_queue()
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        using var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        var sessionTaskId = await browser.AddedTaskAsync(created.SessionId, await Host.AddBankTaskAsync(teamId));

        // Act
        using var response = await browser.DeleteAsync(SessionApi.Session(created.SessionId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var get = await browser.GetAsync(SessionApi.Session(created.SessionId));
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await Should.ThrowAsync<InvalidOperationException>(() => Host.StoredTaskAsync(sessionTaskId));
    }

    [Theory]
    [InlineData("Live")]
    [InlineData("Finished")]
    public async Task A_started_session_can_not_be_deleted(string state)
    {
        // Arrange
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        using var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(staff.User.TeamId!.Value));
        await Host.SetStateAsync(created.SessionId, Enum.Parse<InterviewSessionState>(state));

        // Act
        using var response = await browser.DeleteAsync(SessionApi.Session(created.SessionId));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotPlanned");
        (await browser.SessionAsync(created.SessionId)).State.ShouldBe(Enum.Parse<InterviewSessionState>(state));
    }
}
