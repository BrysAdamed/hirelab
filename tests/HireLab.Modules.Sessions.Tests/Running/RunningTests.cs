using System.Net;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Staff;
using HireLab.Testing.TaskBank;

namespace HireLab.Modules.Sessions.Tests.Running;

public sealed class RunningTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Going_live_makes_the_first_task_current()
    {
        // Arrange
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        Host.IntegrationEvents.Reset();

        // Act
        using var response = await browser.GoLiveAsync(sessionId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var session = await browser.SessionAsync(sessionId);
        session.State.ShouldBe(InterviewSessionState.Live);
        session.Tasks.Select(task => (task.Id, task.State)).ShouldBe([(first, SessionTaskState.Current), (second, SessionTaskState.Pending)]);
        session.QueueRevision.ShouldBe(3);
        Host.IntegrationEvents.OfType<SessionStateChanged>().ShouldBe([new SessionStateChanged(sessionId)]);
        Host.IntegrationEvents.OfType<TaskQueueChanged>().ShouldBe([new TaskQueueChanged(sessionId, 3)]);
    }

    [Fact]
    public async Task Going_live_re_pins_the_first_task_to_the_new_active_version()
    {
        // Arrange
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        var sessionTaskId = await browser.AddedTaskAsync(sessionId, taskId);
        var newVersionId = await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");

        // Act
        await browser.WentLiveAsync(sessionId);

        // Assert
        var stored = await Host.StoredTaskAsync(sessionTaskId);
        stored.State.ShouldBe(SessionTaskState.Current);
        stored.TaskVersionId.ShouldBe(newVersionId);
        stored.Title.ShouldBe("Fake Echo v2");
    }

    [Fact]
    public async Task Going_live_with_an_empty_queue_is_refused()
    {
        // Arrange
        var (browser, sessionId, _, _) = await PlanSessionAsync();

        // Act
        using var response = await browser.GoLiveAsync(sessionId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.EmptyQueue");
        (await browser.SessionAsync(sessionId)).State.ShouldBe(InterviewSessionState.Planned);
        Host.IntegrationEvents.OfType<SessionStateChanged>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Going_live_is_refused_for_an_anonymised_candidate()
    {
        // Arrange
        var (browser, sessionId, teamId, candidateId) = await PlanSessionAsync();
        await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await Host.AnonymiseCandidateAsync(candidateId);

        // Act
        using var response = await browser.GoLiveAsync(sessionId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.CandidateAnonymised");
        (await browser.SessionAsync(sessionId)).State.ShouldBe(InterviewSessionState.Planned);
    }

    [Fact]
    public async Task Going_live_is_refused_for_a_closed_position()
    {
        // Arrange
        var (browser, sessionId, teamId, candidateId) = await PlanSessionAsync();
        await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await Host.ClosePositionOfAsync(candidateId);

        // Act
        using var response = await browser.GoLiveAsync(sessionId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.PositionClosed");
        (await browser.SessionAsync(sessionId)).State.ShouldBe(InterviewSessionState.Planned);
    }

    [Fact]
    public async Task A_live_or_finished_session_can_not_go_live_again()
    {
        // Arrange
        var (browser, liveId, teamId, _) = await PlanSessionAsync();
        await browser.AddedTaskAsync(liveId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(liveId);
        var finishedId = (await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId))).SessionId;
        await browser.AddedTaskAsync(finishedId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(finishedId);
        await browser.FinishedAsync(finishedId);

        // Act
        using var live = await browser.GoLiveAsync(liveId);
        using var finished = await browser.GoLiveAsync(finishedId);

        // Assert
        await live.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotPlanned");
        await finished.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotPlanned");
        (await browser.SessionAsync(finishedId)).State.ShouldBe(InterviewSessionState.Finished);
    }

    [Fact]
    public async Task Marking_the_current_task_done_makes_the_next_one_current()
    {
        // Arrange
        var (browser, sessionId, first, second, third) = await LiveSessionWithThreeTasksAsync();
        Host.IntegrationEvents.Reset();

        // Act
        using var response = await browser.MarkDoneAsync(sessionId, first);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var session = await browser.SessionAsync(sessionId);
        session.State.ShouldBe(InterviewSessionState.Live);
        session.Tasks.Select(task => (task.Id, task.State)).ShouldBe(
            [(first, SessionTaskState.Done), (second, SessionTaskState.Current), (third, SessionTaskState.Pending)]);
        Host.IntegrationEvents.OfType<TaskQueueChanged>().ShouldBe([new TaskQueueChanged(sessionId, session.QueueRevision)]);
        Host.IntegrationEvents.OfType<SessionStateChanged>().ShouldBeEmpty();
    }

    [Fact]
    public async Task The_task_that_becomes_current_is_re_pinned_to_the_new_active_version()
    {
        // Arrange
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        var second = await browser.AddedTaskAsync(sessionId, taskId);
        await browser.WentLiveAsync(sessionId);
        var newVersionId = await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");

        // Act
        await browser.MarkedDoneAsync(sessionId, first);

        // Assert
        var stored = await Host.StoredTaskAsync(second);
        stored.State.ShouldBe(SessionTaskState.Current);
        stored.TaskVersionId.ShouldBe(newVersionId);
    }

    [Fact]
    public async Task After_the_last_task_is_done_the_session_stays_live_without_a_current_task()
    {
        // Arrange
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var only = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);

        // Act
        using var response = await browser.MarkDoneAsync(sessionId, only);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var session = await browser.SessionAsync(sessionId);
        session.State.ShouldBe(InterviewSessionState.Live);
        session.Tasks.ShouldHaveSingleItem().State.ShouldBe(SessionTaskState.Done);
    }

    [Fact]
    public async Task Only_the_current_task_can_be_marked_done()
    {
        // Arrange
        var (browser, sessionId, first, _, third) = await LiveSessionWithThreeTasksAsync();
        await browser.MarkedDoneAsync(sessionId, first);

        // Act
        using var done = await browser.MarkDoneAsync(sessionId, first);
        using var pending = await browser.MarkDoneAsync(sessionId, third);
        using var unknown = await browser.MarkDoneAsync(sessionId, Guid.CreateVersion7());

        // Assert
        await done.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskNotCurrent");
        await pending.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskNotCurrent");
        await unknown.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.SessionTaskNotFound");
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.State)
            .ShouldBe([SessionTaskState.Done, SessionTaskState.Current, SessionTaskState.Pending]);
    }

    [Fact]
    public async Task A_task_can_be_marked_done_only_while_the_session_is_live()
    {
        // Arrange
        var (browser, plannedId, teamId, _) = await PlanSessionAsync();
        var planned = await browser.AddedTaskAsync(plannedId, await Host.AddBankTaskAsync(teamId));
        var finishedId = (await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId))).SessionId;
        var finished = await browser.AddedTaskAsync(finishedId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(finishedId);
        await browser.FinishedAsync(finishedId);

        // Act
        using var inPlanned = await browser.MarkDoneAsync(plannedId, planned);
        using var inFinished = await browser.MarkDoneAsync(finishedId, finished);

        // Assert
        await inPlanned.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotLive");
        await inFinished.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotLive");
    }

    [Fact]
    public async Task Finishing_makes_the_current_task_done_and_leaves_the_pending_ones_not_reached()
    {
        // Arrange
        var (browser, sessionId, first, second, third) = await LiveSessionWithThreeTasksAsync();
        await browser.MarkedDoneAsync(sessionId, first);
        Host.IntegrationEvents.Reset();

        // Act
        using var response = await browser.FinishAsync(sessionId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var session = await browser.SessionAsync(sessionId);
        session.State.ShouldBe(InterviewSessionState.Finished);
        session.Tasks.Select(task => (task.Id, task.State)).ShouldBe(
            [(first, SessionTaskState.Done), (second, SessionTaskState.Done), (third, SessionTaskState.Pending)]);
        Host.IntegrationEvents.OfType<SessionStateChanged>().ShouldBe([new SessionStateChanged(sessionId)]);
        Host.IntegrationEvents.OfType<TaskQueueChanged>().ShouldBe([new TaskQueueChanged(sessionId, session.QueueRevision)]);
    }

    [Fact]
    public async Task Finishing_after_the_last_task_leaves_the_queue_as_it_is()
    {
        // Arrange
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var only = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);
        await browser.MarkedDoneAsync(sessionId, only);
        var revision = (await browser.SessionAsync(sessionId)).QueueRevision;
        Host.IntegrationEvents.Reset();

        // Act
        await browser.FinishedAsync(sessionId);

        // Assert
        var session = await browser.SessionAsync(sessionId);
        session.State.ShouldBe(InterviewSessionState.Finished);
        session.QueueRevision.ShouldBe(revision);
        Host.IntegrationEvents.OfType<SessionStateChanged>().ShouldHaveSingleItem();
        Host.IntegrationEvents.OfType<TaskQueueChanged>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Only_a_live_session_can_be_finished()
    {
        // Arrange
        var (browser, plannedId, teamId, _) = await PlanSessionAsync();
        var finishedId = (await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId))).SessionId;
        await browser.AddedTaskAsync(finishedId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(finishedId);
        await browser.FinishedAsync(finishedId);

        // Act
        using var planned = await browser.FinishAsync(plannedId);
        using var finished = await browser.FinishAsync(finishedId);

        // Assert
        await planned.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotLive");
        await finished.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.NotLive");
        (await browser.SessionAsync(plannedId)).State.ShouldBe(InterviewSessionState.Planned);
    }

    [Fact]
    public async Task Another_teams_session_can_not_be_run()
    {
        // Arrange
        var (otherBrowser, otherSessionId, otherTeamId, _) = await PlanSessionAsync();
        var queuedId = await otherBrowser.AddedTaskAsync(otherSessionId, await Host.AddBankTaskAsync(otherTeamId));
        var (browser, _, _, _) = await PlanSessionAsync();

        // Act
        using var goLive = await browser.GoLiveAsync(otherSessionId);
        using var markDone = await browser.MarkDoneAsync(otherSessionId, queuedId);
        using var finish = await browser.FinishAsync(otherSessionId);

        // Assert
        await goLive.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await markDone.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await finish.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        (await otherBrowser.SessionAsync(otherSessionId)).State.ShouldBe(InterviewSessionState.Planned);
    }

    [Fact]
    public async Task A_manager_runs_a_session_an_interviewer_planned()
    {
        // Arrange
        var (_, sessionId, teamId, _) = await PlanSessionAsync();
        var manager = await Host.CreateUserAsync(UserBuilder.Manager(teamId));
        var managerBrowser = await Host.CreateBrowserAsync(await Host.SignInAsync(manager));
        var only = await managerBrowser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));

        // Act
        await managerBrowser.WentLiveAsync(sessionId);
        await managerBrowser.MarkedDoneAsync(sessionId, only);
        await managerBrowser.FinishedAsync(sessionId);

        // Assert
        (await managerBrowser.SessionAsync(sessionId)).State.ShouldBe(InterviewSessionState.Finished);
    }

    private async Task<(BrowserSession Browser, InterviewSessionId SessionId, Guid First, Guid Second, Guid Third)> LiveSessionWithThreeTasksAsync()
    {
        var (browser, sessionId, teamId, _) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var third = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);
        return (browser, sessionId, first, second, third);
    }

    private async Task<(BrowserSession Browser, InterviewSessionId SessionId, TeamId TeamId, CandidateId CandidateId)> PlanSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var candidateId = await Host.CreateCandidateAsync(teamId);
        var created = await browser.CreatedAsync(candidateId);
        return (browser, created.SessionId, teamId, candidateId);
    }
}
