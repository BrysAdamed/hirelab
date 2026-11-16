using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HireLab.Modules.Sessions.CandidateAccess;
using HireLab.Modules.Sessions.CandidateAccess.SaveResponse;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Sessions;
using HireLab.Testing.TaskBank;
using Microsoft.AspNetCore.Http;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

public sealed class ResponseTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Saving_stores_the_response_raises_its_revision_and_raises_an_event()
    {
        // Arrange
        var session = await LiveSessionAsync();
        Host.IntegrationEvents.Reset();

        // Act
        var first = await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake first draft" }, 0);
        var second = await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake second draft" }, first);

        // Assert
        first.ShouldBe(1);
        second.ShouldBe(2);
        var current = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull();
        current.Revision.ShouldBe(2);
        current.Response.ShouldNotBeNull().GetProperty("echo").GetString().ShouldBe("fake second draft");
        Host.IntegrationEvents.OfType<ResponseUpdated>().ShouldBe(
        [
            new ResponseUpdated(session.Id, new SessionTaskId(session.First), 1),
            new ResponseUpdated(session.Id, new SessionTaskId(session.First), 2),
        ]);
    }

    [Fact]
    public async Task The_current_task_has_no_response_and_revision_0_before_the_first_save()
    {
        // Arrange
        var session = await LiveSessionAsync();

        // Act
        var current = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull();

        // Assert
        current.Response.ShouldBeNull();
        current.Revision.ShouldBe(0);
    }

    [Fact]
    public async Task Re_opening_the_link_loads_the_saved_response()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake saved answer" }, 0);
        using var reopened = await Host.CreateCandidateBrowserAsync();

        // Act
        await reopened.ExchangedAsync(session.Token);

        // Assert
        var current = (await reopened.InterviewAsync()).CurrentTask.ShouldNotBeNull();
        current.Revision.ShouldBe(1);
        current.Response.ShouldNotBeNull().GetProperty("echo").GetString().ShouldBe("fake saved answer");
    }

    [Fact]
    public async Task The_response_is_stored_as_the_task_type_serialises_it()
    {
        // Arrange
        var session = await LiveSessionAsync();

        // Act
        using var response = await session.Candidate.PutAsJsonAsync(
            CandidateApi.Response,
            new Dictionary<string, object>
            {
                ["sessionTaskId"] = session.First,
                ["response"] = new Dictionary<string, string> { ["Echo"] = "fake echo" },
                ["revision"] = 0,
            });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Response.ShouldNotBeNull();
        stored.GetRawText().ShouldBe("""{"echo":"fake echo"}""");
    }

    [Fact]
    public async Task A_save_based_on_an_older_revision_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake from tab one" }, 0);
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake from tab one again" }, 1);
        Host.IntegrationEvents.Reset();

        // Act
        using var stale = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake from tab two" }, 1);
        using var ahead = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake from the future" }, 5);

        // Assert
        await stale.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Response.StaleRevision");
        await ahead.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Response.StaleRevision");
        var current = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull();
        current.Revision.ShouldBe(2);
        current.Response.ShouldNotBeNull().GetProperty("echo").GetString().ShouldBe("fake from tab one again");
        Host.IntegrationEvents.OfType<ResponseUpdated>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_first_save_that_names_a_revision_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake echo" }, 1);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Response.StaleRevision");
    }

    [Fact]
    public async Task A_response_larger_than_1_MB_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();
        var tooLong = new string('x', Response.MaxBytes);

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, new { echo = tooLong }, 0);

        // Assert
        await ShouldBeInvalidAsync(response, "response");
        (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Revision.ShouldBe(0);
    }

    [Fact]
    public async Task A_response_that_does_not_match_the_task_type_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();

        // Act
        using var unknownProperty = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake", extra = "fake" }, 0);
        using var missingProperty = await session.Candidate.SaveResponseAsync(session.First, new { }, 0);

        // Assert
        await ShouldBeInvalidAsync(unknownProperty, "response");
        await ShouldBeInvalidAsync(missingProperty, "response");
        (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Revision.ShouldBe(0);
    }

    [Fact]
    public async Task Saving_to_a_pending_task_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.Second, new { echo = "fake echo" }, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskNotCurrent");
    }

    [Fact]
    public async Task Saving_to_a_done_task_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();
        var revision = await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake echo" }, 0);
        await session.Staff.MarkedDoneAsync(session.Id, session.First);

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake late edit" }, revision);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskNotCurrent");
    }

    [Fact]
    public async Task Saving_while_the_session_is_planned_is_refused()
    {
        // Arrange
        var session = await PlannedSessionAsync();
        using var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);

        // Act
        using var response = await candidate.SaveResponseAsync(session.First, new { echo = "fake echo" }, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotLive");
    }

    [Fact]
    public async Task Saving_after_the_session_finished_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Staff.FinishedAsync(session.Id);

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake echo" }, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ProblemAsync()).Code().ShouldBe("CandidateSession.Ended");
    }

    [Fact]
    public async Task Saving_to_a_task_of_another_session_is_refused()
    {
        // Arrange
        var session = await LiveSessionAsync();
        var other = await LiveSessionAsync();

        // Act
        using var response = await session.Candidate.SaveResponseAsync(other.First, new { echo = "fake echo" }, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.SessionTaskNotFound");
        (await other.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Revision.ShouldBe(0);
    }

    [Fact]
    public async Task Saving_requires_the_antiforgery_token()
    {
        // Arrange
        var session = await LiveSessionAsync();
        session.Candidate.SendsAntiforgeryHeader = false;

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, new { echo = "fake echo" }, 0);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Antiforgery.InvalidToken");
    }

    [Fact]
    public async Task Staff_read_the_response_of_the_current_task_and_of_a_done_one()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake first answer" }, 0);

        // Act
        var current = await session.Staff.SessionTaskAsync(session.Id, session.First);
        await session.Staff.MarkedDoneAsync(session.Id, session.First);
        var done = await session.Staff.SessionTaskAsync(session.Id, session.First);
        var pending = await session.Staff.SessionTaskAsync(session.Id, session.Second);

        // Assert
        current.ResponseRevision.ShouldBe(1);
        current.Response.ShouldNotBeNull().GetProperty("echo").GetString().ShouldBe("fake first answer");
        current.CandidateContent.GetProperty("phrase").GetString().ShouldNotBeNullOrEmpty();
        current.StaffContent.GetProperty("matchCriterionId").GetString().ShouldNotBeNullOrEmpty();
        done.State.ShouldBe(SessionTaskState.Done);
        done.Response.ShouldNotBeNull().GetProperty("echo").GetString().ShouldBe("fake first answer");
        pending.Response.ShouldBeNull();
        pending.ResponseRevision.ShouldBe(0);
    }

    [Fact]
    public async Task Staff_of_another_team_cannot_read_the_response()
    {
        // Arrange
        var session = await LiveSessionAsync();
        await session.Candidate.SavedResponseAsync(session.First, new { echo = "fake private answer" }, 0);
        using var otherTeam = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Interviewer));

        // Act
        using var response = await otherTeam.GetAsync(SessionApi.QueuedTask(session.Id, session.First));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
    }

    [Fact]
    public void The_save_command_and_request_never_show_the_response_content()
    {
        // Arrange
        using var content = JsonDocument.Parse("""{"echo":"fake confidential answer"}""");
        var cookie = new CandidateCookie(InterviewSessionId.New(), CandidateLinkId.New());

        // Act
        var command = new SaveResponseCommand(cookie, SessionTaskId.New(), content.RootElement, 1).ToString();
        var request = new SaveResponseRequest(Guid.CreateVersion7(), content.RootElement, 1).ToString();

        // Assert
        command.ShouldNotContain("fake confidential answer");
        request.ShouldNotContain("fake confidential answer");
    }

    private static async Task ShouldBeInvalidAsync(HttpResponseMessage response, string field)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(field);
    }

    private async Task<TestSession> PlannedSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        var first = await browser.AddedTaskAsync(created.SessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(created.SessionId, await Host.AddBankTaskAsync(teamId));
        var token = await browser.IssuedCandidateLinkAsync(created.Id);
        return new TestSession(browser, null!, created.SessionId, token, first, second);
    }

    private async Task<TestSession> LiveSessionAsync()
    {
        var session = await PlannedSessionAsync();
        await session.Staff.WentLiveAsync(session.Id);
        var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(session.Token);
        return session with { Candidate = candidate };
    }

    private sealed record TestSession(BrowserSession Staff, BrowserSession Candidate, InterviewSessionId Id, string Token, Guid First, Guid Second);
}
