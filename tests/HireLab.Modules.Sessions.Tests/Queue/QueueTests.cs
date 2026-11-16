using System.Net;
using System.Text.Json;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Users;
using HireLab.Testing.Positions;
using HireLab.Testing.Staff;
using HireLab.Testing.TaskBank;
using HireLab.Testing.TaskTypes;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Sessions.Tests.Queue;

public sealed class QueueTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Queueing_a_task_pins_its_active_version_with_a_snapshot()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo Task");
        var activeVersion = await ActiveVersionAsync(teamId, taskId);

        // Act
        var sessionTaskId = await browser.AddedTaskAsync(sessionId, taskId);

        // Assert
        var session = await browser.SessionAsync(sessionId);
        var queued = session.Tasks.ShouldHaveSingleItem();
        queued.Id.ShouldBe(sessionTaskId);
        queued.TaskId.ShouldBe(taskId.Value);
        queued.State.ShouldBe(SessionTaskState.Pending);
        queued.VersionNumber.ShouldBe(1);
        queued.TaskType.ShouldBe(TestEchoTaskType.TaskTypeKey);
        queued.Title.ShouldBe("Fake Echo Task");
        session.QueueRevision.ShouldBe(1);

        var stored = await Host.StoredTaskAsync(sessionTaskId);
        stored.TaskVersionId.ShouldBe(activeVersion.VersionId);
        stored.ContentSchemaVersion.ShouldBe(activeVersion.ContentSchemaVersion);
        stored.Summary.ShouldBe(activeVersion.Summary);
        stored.ReferenceAnswer.ShouldBe(activeVersion.ReferenceAnswer);
        stored.InterviewerNotes.ShouldBe(activeVersion.InterviewerNotes);
        stored.Rubric.Criteria.Count.ShouldBe(activeVersion.Rubric.Criteria.Count);
        stored.Rubric.Criteria[1].ShouldBe(activeVersion.Rubric.Criteria[1]);
        stored.CandidateContent.Deserialize<TestEchoCandidateContent>(JsonSerializerOptions.Web)
            .ShouldBe(activeVersion.CandidateContent.Deserialize<TestEchoCandidateContent>(JsonSerializerOptions.Web));
        stored.StaffContent.Deserialize<TestEchoStaffContent>(JsonSerializerOptions.Web).ShouldBe(new TestEchoStaffContent("fake-echo"));
    }

    [Fact]
    public async Task Another_teams_global_task_can_be_queued_but_its_team_task_can_not()
    {
        // Arrange
        var (browser, sessionId, _) = await PlanSessionAsync();
        var otherTeam = await Host.CreateTeamAsync();
        var globalTaskId = await Host.AddBankTaskAsync(otherTeam.Id, global: true);
        var teamTaskId = await Host.AddBankTaskAsync(otherTeam.Id);

        // Act
        using var global = await browser.AddTaskAsync(sessionId, globalTaskId);
        using var team = await browser.AddTaskAsync(sessionId, teamTaskId);

        // Assert
        global.StatusCode.ShouldBe(HttpStatusCode.Created);
        await team.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Task.NotFound");
    }

    [Fact]
    public async Task An_archived_task_can_not_be_queued()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId);
        await Host.ArchiveBankTaskAsync(taskId);

        // Act
        using var response = await browser.AddTaskAsync(sessionId, taskId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Task.NotFound");
    }

    [Fact]
    public async Task A_task_is_queued_at_most_once()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId);
        await browser.AddedTaskAsync(sessionId, taskId);

        // Act
        using var response = await browser.AddTaskAsync(sessionId, taskId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskAlreadyQueued");
    }

    [Fact]
    public async Task The_queue_of_a_finished_session_can_not_be_changed()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var doneId = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var notReachedId = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);
        await browser.FinishedAsync(sessionId);

        // Act
        using var add = await browser.AddTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        using var remove = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, notReachedId));
        using var reorder = await browser.ReorderAsync(sessionId, doneId, notReachedId);

        // Assert
        await add.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.Finished");
        await remove.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.Finished");
        await reorder.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.Finished");
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.Id).ShouldBe([doneId, notReachedId]);
    }

    [Fact]
    public async Task While_live_a_task_is_added_after_the_others()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);
        await browser.MarkedDoneAsync(sessionId, first);

        // Act
        var added = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));

        // Assert
        var session = await browser.SessionAsync(sessionId);
        session.Tasks.Select(task => (task.Id, task.State)).ShouldBe([(first, SessionTaskState.Done), (added, SessionTaskState.Pending)]);
    }

    [Fact]
    public async Task While_live_only_a_task_that_has_not_started_can_be_removed()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var done = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var current = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var pending = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);
        await browser.MarkedDoneAsync(sessionId, done);

        // Act
        using var removeDone = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, done));
        using var removeCurrent = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, current));
        using var removePending = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, pending));

        // Assert
        await removeDone.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskStarted");
        await removeCurrent.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskStarted");
        removePending.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.Id).ShouldBe([done, current]);
    }

    [Fact]
    public async Task While_live_tasks_that_have_not_started_are_reordered_behind_the_started_ones()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var current = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var third = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        await browser.WentLiveAsync(sessionId);

        // Act
        using var reorderPending = await browser.ReorderAsync(sessionId, current, third, second);
        using var moveCurrent = await browser.ReorderAsync(sessionId, second, current, third);

        // Assert
        reorderPending.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await moveCurrent.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskStarted");
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.Id).ShouldBe([current, third, second]);
    }

    [Fact]
    public async Task Every_queue_change_raises_the_queue_changed_event_with_the_new_revision()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();

        // Act
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        using var reorder = await browser.ReorderAsync(sessionId, second, first);
        using var remove = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, first));

        // Assert
        Host.IntegrationEvents.OfType<Contracts.TaskQueueChanged>().Select(raised => raised.QueueRevision).ShouldBe([1, 2, 3, 4]);
        Host.IntegrationEvents.OfType<Contracts.TaskQueueChanged>().ShouldAllBe(raised => raised.SessionId == sessionId);
    }

    [Fact]
    public async Task A_refused_queue_change_raises_no_event()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId);
        await browser.AddedTaskAsync(sessionId, taskId);
        Host.IntegrationEvents.Reset();

        // Act
        using var response = await browser.AddTaskAsync(sessionId, taskId);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.TaskAlreadyQueued");
        Host.IntegrationEvents.OfType<Contracts.TaskQueueChanged>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_queued_task_is_read_with_the_staff_material_of_its_pinned_version()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo Task");
        var sessionTaskId = await browser.AddedTaskAsync(sessionId, taskId);
        var stored = await Host.StoredTaskAsync(sessionTaskId);

        // Act
        var task = await browser.SessionTaskAsync(sessionId, sessionTaskId);

        // Assert
        task.Id.ShouldBe(sessionTaskId);
        task.TaskId.ShouldBe(taskId.Value);
        task.State.ShouldBe(SessionTaskState.Pending);
        task.VersionNumber.ShouldBe(1);
        task.TaskType.ShouldBe(TestEchoTaskType.TaskTypeKey);
        task.Title.ShouldBe("Fake Echo Task");
        task.Summary.ShouldBe(stored.Summary);
        task.ReferenceAnswer.ShouldBe(stored.ReferenceAnswer);
        task.InterviewerNotes.ShouldBe(stored.InterviewerNotes);
    }

    [Fact]
    public async Task A_task_of_another_session_or_team_is_not_found()
    {
        // Arrange
        var (otherBrowser, otherSessionId, otherTeamId) = await PlanSessionAsync();
        var otherTaskId = await otherBrowser.AddedTaskAsync(otherSessionId, await Host.AddBankTaskAsync(otherTeamId));
        var (browser, sessionId, _) = await PlanSessionAsync();

        // Act
        using var otherTeam = await browser.GetAsync(SessionApi.QueuedTask(otherSessionId, otherTaskId));
        using var otherSession = await browser.GetAsync(SessionApi.QueuedTask(sessionId, otherTaskId));

        // Assert
        await otherTeam.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await otherSession.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.SessionTaskNotFound");
    }

    [Fact]
    public async Task Removing_a_task_closes_the_gap_in_the_queue()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var third = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));

        // Act
        using var response = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, second));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.Id).ShouldBe([first, third]);
        (await Host.StoredTaskAsync(third)).Position.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_a_task_that_is_not_queued_is_not_found()
    {
        // Arrange
        var (browser, sessionId, _) = await PlanSessionAsync();

        // Act
        using var response = await browser.DeleteAsync(SessionApi.QueuedTask(sessionId, Guid.CreateVersion7()));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.SessionTaskNotFound");
    }

    [Fact]
    public async Task The_queue_is_reordered()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var third = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));

        // Act
        using var response = await browser.ReorderAsync(sessionId, third, first, second);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var session = await browser.SessionAsync(sessionId);
        session.Tasks.Select(task => task.Id).ShouldBe([third, first, second]);
        session.QueueRevision.ShouldBe(4);
    }

    [Fact]
    public async Task A_reorder_that_does_not_list_the_queue_exactly_is_refused()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var first = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));
        var second = await browser.AddedTaskAsync(sessionId, await Host.AddBankTaskAsync(teamId));

        // Act
        using var missing = await browser.ReorderAsync(sessionId, second);
        using var duplicated = await browser.ReorderAsync(sessionId, second, second);
        using var unknown = await browser.ReorderAsync(sessionId, second, Guid.CreateVersion7());

        // Assert
        await missing.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.OrderMismatch");
        await duplicated.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.OrderMismatch");
        await unknown.ShouldBeProblemAsync(HttpStatusCode.Conflict, "InterviewSession.OrderMismatch");
        (await browser.SessionAsync(sessionId)).Tasks.Select(task => task.Id).ShouldBe([first, second]);
    }

    [Fact]
    public async Task Another_teams_session_queue_is_not_found()
    {
        // Arrange
        var (otherBrowser, otherSessionId, otherTeamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(otherTeamId, global: true);
        var queuedId = await otherBrowser.AddedTaskAsync(otherSessionId, taskId);
        var (browser, _, _) = await PlanSessionAsync();

        // Act
        using var add = await browser.AddTaskAsync(otherSessionId, taskId);
        using var remove = await browser.DeleteAsync(SessionApi.QueuedTask(otherSessionId, queuedId));
        using var reorder = await browser.ReorderAsync(otherSessionId, queuedId);

        // Assert
        await add.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await remove.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        await reorder.ShouldBeProblemAsync(HttpStatusCode.NotFound, "InterviewSession.NotFound");
        (await otherBrowser.SessionAsync(otherSessionId)).Tasks.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Opening_the_queue_re_pins_a_pending_task_to_the_new_active_version()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        var sessionTaskId = await browser.AddedTaskAsync(sessionId, taskId);
        var newVersionId = await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");

        // Act
        var session = await browser.SessionAsync(sessionId);

        // Assert
        var queued = session.Tasks.ShouldHaveSingleItem();
        queued.VersionNumber.ShouldBe(2);
        queued.Title.ShouldBe("Fake Echo v2");
        (await Host.StoredTaskAsync(sessionTaskId)).TaskVersionId.ShouldBe(newVersionId);
    }

    [Fact]
    public async Task Opening_the_queue_keeps_the_pin_of_an_archived_task()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        await browser.AddedTaskAsync(sessionId, taskId);
        await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");
        await Host.ArchiveBankTaskAsync(taskId);

        // Act
        var session = await browser.SessionAsync(sessionId);

        // Assert
        var queued = session.Tasks.ShouldHaveSingleItem();
        queued.VersionNumber.ShouldBe(1);
        queued.Title.ShouldBe("Fake Echo v1");
    }

    [Fact]
    public async Task Opening_the_queue_keeps_the_pin_of_a_task_whose_type_is_no_longer_registered()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        var sessionTaskId = await browser.AddedTaskAsync(sessionId, taskId);
        await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");
        await Host.ExecuteSessionsSqlAsync($"UPDATE sessions.session_tasks SET task_type_key = 'retired-fake-type' WHERE id = {sessionTaskId}");

        // Act
        var session = await browser.SessionAsync(sessionId);

        // Assert
        session.Tasks.ShouldHaveSingleItem().VersionNumber.ShouldBe(1);
    }

    [Fact]
    public async Task Opening_the_queue_never_re_pins_a_task_that_has_started()
    {
        // Arrange
        var (browser, sessionId, teamId) = await PlanSessionAsync();
        var taskId = await Host.AddBankTaskAsync(teamId, "Fake Echo v1");
        await browser.AddedTaskAsync(sessionId, taskId);
        await browser.WentLiveAsync(sessionId);
        await Host.PublishNewVersionAsync(taskId, "Fake Echo v2");

        // Act
        var session = await browser.SessionAsync(sessionId);

        // Assert
        var queued = session.Tasks.ShouldHaveSingleItem();
        queued.State.ShouldBe(SessionTaskState.Current);
        queued.VersionNumber.ShouldBe(1);
    }

    private async Task<(BrowserSession Browser, Contracts.InterviewSessionId SessionId, TeamId TeamId)> PlanSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        return (browser, created.SessionId, teamId);
    }

    private async Task<TaskVersionSnapshot> ActiveVersionAsync(TeamId teamId, TaskId taskId)
    {
        var reader = await Host.CreateUserAsync(UserBuilder.Interviewer(teamId));
        await using var scope = Host.CreateScopeAs(reader);
        return (await scope.Services.GetRequiredService<IActiveTaskVersionQuery>().FindAsync(taskId, CancellationToken)).ShouldNotBeNull();
    }
}
