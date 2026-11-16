using System.Net;
using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Sessions;
using HireLab.Testing.TaskBank;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

public sealed class CandidateAccessTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Opening_the_link_of_a_planned_session_shows_the_waiting_page()
    {
        // Arrange
        var session = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();

        // Act
        using var response = await candidate.ExchangeAsync(session.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldNotBeNull();
        candidate.Cookie(AntiforgeryTokens.CandidateCookieName).ShouldNotBeNull();
        (await candidate.InterviewAsync()).ShouldBe(new InterviewView("Waiting", null));
    }

    [Fact]
    public async Task Opening_the_link_of_a_live_session_shows_only_the_current_task()
    {
        // Arrange
        var session = await PlanSessionAsync();
        var first = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.WentLiveAsync(session.Id);
        using var candidate = await Host.CreateCandidateBrowserAsync();

        // Act
        await candidate.ExchangedAsync(session.Token);

        // Assert
        var interview = await candidate.InterviewAsync();
        interview.Stage.ShouldBe("CurrentTask");
        var current = interview.CurrentTask.ShouldNotBeNull();
        current.Id.ShouldBe(first);
        current.TaskType.ShouldBe(TestEchoTaskType.TaskTypeKey);
        var stored = await Host.StoredTaskAsync(first);
        current.Content.GetProperty("phrase").GetString().ShouldBe(stored.CandidateContent.GetProperty("phrase").GetString());
    }

    [Fact]
    public async Task The_waiting_page_turns_into_the_current_task_when_the_session_goes_live()
    {
        // Arrange
        var session = await PlanSessionAsync();
        var first = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        (await candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Id.ShouldBe(first);
    }

    [Fact]
    public async Task Marking_the_task_done_moves_the_candidate_to_the_next_one_and_then_to_the_closing_screen()
    {
        // Arrange
        var session = await PlanSessionAsync();
        var first = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        var second = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.WentLiveAsync(session.Id);
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        await session.Staff.MarkedDoneAsync(session.Id, first);
        var next = await candidate.InterviewAsync();
        await session.Staff.MarkedDoneAsync(session.Id, second);
        var closing = await candidate.InterviewAsync();

        // Assert
        next.CurrentTask.ShouldNotBeNull().Id.ShouldBe(second);
        closing.ShouldBe(new InterviewView("Closing", null));
    }

    [Fact]
    public async Task The_link_of_a_finished_session_gives_no_cookie()
    {
        // Arrange
        var session = await PlanSessionAsync();
        await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.WentLiveAsync(session.Id);
        await session.Staff.FinishedAsync(session.Id);
        using var candidate = await Host.CreateCandidateBrowserAsync();

        // Act
        using var response = await candidate.ExchangeAsync(session.Token);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "CandidateSession.Ended");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
    }

    [Fact]
    public async Task The_cookie_is_rejected_once_the_session_is_finished()
    {
        // Arrange
        var session = await PlanSessionAsync();
        await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.WentLiveAsync(session.Id);
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        await session.Staff.FinishedAsync(session.Id);

        // Assert
        using var response = await candidate.GetAsync(CandidateApi.Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemAsync()).Code().ShouldBe("CandidateSession.Ended");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
    }

    [Fact]
    public async Task The_cookie_is_rejected_once_the_planned_session_is_deleted()
    {
        // Arrange
        var session = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        using (var deleted = await session.Staff.DeleteAsync(SessionApi.Session(session.Id)))
        {
            deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Assert
        using var response = await candidate.GetAsync(CandidateApi.Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemAsync()).Code().ShouldBe("CandidateLink.Invalid");
    }

    [Fact]
    public async Task The_cookie_expires_after_12_hours()
    {
        // Arrange
        var session = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);
        Host.Time.Advance(TimeSpan.FromHours(11.9));
        (await candidate.InterviewAsync()).Stage.ShouldBe("Waiting");

        // Act
        Host.Time.Advance(TimeSpan.FromHours(0.1) + TimeSpan.FromSeconds(1));

        // Assert
        using var response = await candidate.GetAsync(CandidateApi.Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_unknown_token_gives_no_cookie()
    {
        // Arrange
        using var candidate = await Host.CreateCandidateBrowserAsync();

        // Act
        using var response = await candidate.ExchangeAsync("fake-token-that-was-never-issued");

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "CandidateLink.Invalid");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
    }

    [Fact]
    public async Task Candidate_responses_carry_no_staff_content_and_no_other_tasks()
    {
        // Arrange
        var session = await PlanSessionAsync();
        var first = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId, "Fake Current Title"));
        var second = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId, "Fake Next Title"));
        await session.Staff.WentLiveAsync(session.Id);
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        using var response = await candidate.GetAsync(CandidateApi.Interview);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.EnumerateObject().Select(property => property.Name).ShouldBe(["stage", "currentTask"]);
        json.RootElement.GetProperty("currentTask").EnumerateObject().Select(property => property.Name).ShouldBe(["id", "taskType", "content", "response", "revision"]);
        var stored = await Host.StoredTaskAsync(first);
        body.ShouldNotContain(stored.ReferenceAnswer);
        body.ShouldNotContain(stored.InterviewerNotes!);
        body.ShouldNotContain(stored.StaffContent.GetProperty("matchCriterionId").GetString()!);
        body.ShouldNotContain("Fake Current Title");
        body.ShouldNotContain("Fake Next Title");
        body.ShouldNotContain(second.ToString());
    }

    [Fact]
    public async Task The_exchange_requires_the_antiforgery_token()
    {
        // Arrange
        var session = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        candidate.SendsAntiforgeryHeader = false;

        // Act
        using var response = await candidate.ExchangeAsync(session.Token);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Antiforgery.InvalidToken");
        candidate.Cookie(TestCandidateAccess.CookieName).ShouldBeNull();
    }

    [Fact]
    public async Task The_exchange_issues_an_antiforgery_token_for_the_signed_in_candidate()
    {
        // Arrange
        var session = await PlanSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        var anonymousToken = candidate.Cookie(AntiforgeryTokens.CandidateCookieName);
        await candidate.ExchangedAsync(session.Token);

        // Act
        using var response = await candidate.ExchangeAsync(session.Token);

        // Assert
        candidate.Cookie(AntiforgeryTokens.CandidateCookieName).ShouldNotBe(anonymousToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Opening_the_link_in_the_staff_browser_keeps_the_staff_antiforgery_token_working()
    {
        // Arrange
        var session = await PlanSessionAsync();
        await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        var staffToken = session.Staff.Cookie(AntiforgeryTokens.CookieName);
        using (var antiforgery = await session.Staff.GetAsync(TestCandidateAccess.AntiforgeryUri))
        {
            antiforgery.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        await session.Staff.ExchangedAsync(session.Token);

        // Act
        await session.Staff.WentLiveAsync(session.Id);

        // Assert
        session.Staff.Cookie(AntiforgeryTokens.CookieName).ShouldBe(staffToken);
        session.Staff.Cookie(AntiforgeryTokens.CandidateCookieName).ShouldNotBeNull();
    }

    [Fact]
    public async Task The_candidate_keeps_saving_after_staff_get_a_new_antiforgery_token_in_the_same_browser()
    {
        // Arrange
        var session = await PlanSessionAsync();
        var first = await session.Staff.AddedTaskAsync(session.Id, await Host.AddBankTaskAsync(session.TeamId));
        await session.Staff.WentLiveAsync(session.Id);
        using (var antiforgery = await session.Staff.GetAsync(TestCandidateAccess.AntiforgeryUri))
        {
            antiforgery.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        await session.Staff.ExchangedAsync(session.Token);
        await session.Staff.RefreshAntiforgeryTokenAsync();

        // Act
        var revision = await session.Staff.SavedResponseAsync(first, new { echo = "fake answer from a shared browser" }, 0);

        // Assert
        revision.ShouldBe(1);
    }

    [Fact]
    public async Task The_exchange_is_rate_limited()
    {
        // Arrange
        using var candidate = await Host.CreateCandidateBrowserAsync();
        for (var attempt = 0; attempt < TestCandidateAccess.ExchangePermitLimit; attempt++)
        {
            using var allowed = await candidate.ExchangeAsync($"fake-token-{attempt}");
            allowed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // Act
        using var response = await candidate.ExchangeAsync("fake-token-one-too-many");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task The_staff_cookie_gives_no_candidate_access()
    {
        // Arrange
        var session = await PlanSessionAsync();

        // Act
        using var response = await session.Staff.GetAsync(CandidateApi.Interview);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<PlannedSession> PlanSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        var token = await browser.IssuedCandidateLinkAsync(created.Id);
        return new PlannedSession(browser, created.SessionId, teamId, token);
    }

    private sealed record PlannedSession(BrowserSession Staff, InterviewSessionId Id, TeamId TeamId, string Token);
}
