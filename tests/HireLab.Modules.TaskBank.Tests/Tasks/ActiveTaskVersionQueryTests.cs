using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.Testing.Staff;
using HireLab.Testing.TaskTypes;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.TaskBank.Tests.Tasks;

public sealed class ActiveTaskVersionQueryTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Own_teams_task_is_found(StaffRole role)
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(team.Id));
        var user = await Host.CreateUserAsync(role == StaffRole.Manager ? UserBuilder.Manager(team.Id) : UserBuilder.Interviewer(team.Id));

        // Act
        var snapshot = await FindAsAsync(user, taskId);

        // Assert
        snapshot.ShouldNotBeNull().TaskId.ShouldBe(taskId);
    }

    [Fact]
    public async Task Another_teams_team_task_is_not_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(otherTeam.Id, TaskVisibility.Team));
        var manager = await CreateManagerAsync();

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task Another_teams_global_task_and_a_system_task_are_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var globalTaskId = await Host.AddTaskAsync(TestTasks.Team(otherTeam.Id, TaskVisibility.Global));
        var systemTaskId = await Host.AddTaskAsync(TestTasks.System("fake-echo-task", new TaskVersionBuilder()));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer((await Host.CreateTeamAsync()).Id));

        // Act
        var globalTask = await FindAsAsync(interviewer, globalTaskId);
        var systemTask = await FindAsAsync(interviewer, systemTaskId);

        // Assert
        globalTask.ShouldNotBeNull();
        systemTask.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_admin_finds_no_task()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var taskIds = new[]
        {
            await Host.AddTaskAsync(TestTasks.Team(team.Id, TaskVisibility.Team)),
            await Host.AddTaskAsync(TestTasks.Team(team.Id, TaskVisibility.Global)),
            await Host.AddTaskAsync(TestTasks.System("fake-echo-task", new TaskVersionBuilder())),
        };
        var admin = await Host.CreateUserAsync(UserBuilder.Admin());

        // Act
        var snapshots = new List<TaskVersionSnapshot?>();
        foreach (var taskId in taskIds)
        {
            snapshots.Add(await FindAsAsync(admin, taskId));
        }

        // Assert
        snapshots.ShouldAllBe(snapshot => snapshot == null);
    }

    [Fact]
    public async Task Code_outside_a_request_finds_no_task()
    {
        // Arrange
        var taskId = await Host.AddTaskAsync(TestTasks.System("fake-echo-task", new TaskVersionBuilder()));
        await using var scope = Host.Services.CreateAsyncScope();

        // Act
        var snapshot = await scope.ServiceProvider.GetRequiredService<IActiveTaskVersionQuery>().FindAsync(taskId, CancellationToken);

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task An_archived_task_is_not_found()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value));
        await Host.ChangeTaskAsync(taskId, task => task.Archive(manager.Id, Host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue());

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task A_task_with_only_a_draft_is_not_found()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value, publishedVersions: 0));

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task The_newest_published_version_is_active_and_a_draft_never_is()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value, publishedVersions: 2, withDraft: true));

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldNotBeNull().Number.ShouldBe(2);
    }

    [Fact]
    public async Task A_switched_active_version_is_found_instead_of_the_newest()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var task = TestTasks.Team(manager.TeamId!.Value, publishedVersions: 3);
        var second = task.Versions.Single(version => version.Number == 2);
        var taskId = await Host.AddTaskAsync(task);
        await Host.ChangeTaskAsync(taskId, task => task.SwitchActiveVersion(second.Id).IsSuccess.ShouldBeTrue());

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldNotBeNull().VersionId.ShouldBe(second.Id);
    }

    [Fact]
    public async Task A_system_task_uses_its_newest_version()
    {
        // Arrange
        var task = TestTasks.System(
            "fake-echo-task",
            new TaskVersionBuilder().WithTitle("Fake echo v1"),
            new TaskVersionBuilder().WithTitle("Fake echo v2"));
        var taskId = await Host.AddTaskAsync(task);
        await Host.ChangeTaskAsync(
            taskId,
            task => task.AddSystemVersion(TestTasks.Valid(new TaskVersionBuilder().WithTitle("Fake echo v3")), Host.Time.GetUtcNow()));

        // Act
        var snapshot = await FindAsAsync(await CreateManagerAsync(), taskId);

        // Assert
        snapshot.ShouldNotBeNull().Number.ShouldBe(3);
        snapshot.Title.ShouldBe("Fake echo v3");
    }

    [Fact]
    public async Task The_snapshot_holds_the_active_versions_content_reference_answer_notes_and_rubric()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var rubric = new RubricCriterion[]
        {
            new ScaleCriterion("fake-clarity", "Fake clarity", "How clear the fake echo is.", Weight: 4, [new ScaleLevel(4, "Crystal fake.")]),
            new CheckCriterion("fake-echo", "Fake echo", "Echoed the phrase?", CheckAnswer.Yes, Critical: true),
        };
        var details = new TaskVersionBuilder()
            .WithTitle("Fake snapshot task")
            .WithSummary("A made-up summary.")
            .WithPhrase("Fake phrase to echo")
            .WithReferenceAnswer("Echo the fake phrase.")
            .WithInterviewerNotes("Ask about fake echoes.")
            .WithRubric(rubric);
        var task = InterviewTask.CreateForTeam(manager.TeamId!.Value, TaskVisibility.Team, TestTasks.Valid(details), manager.Id);
        var version = task.PublishDraft(makeActive: true, manager.Id, Host.Time.GetUtcNow()).Value;
        var taskId = await Host.AddTaskAsync(task);

        // Act
        var snapshot = await FindAsAsync(manager, taskId);

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.VersionId.ShouldBe(version.Id);
        snapshot.Number.ShouldBe(1);
        snapshot.TaskTypeKey.ShouldBe(TestEchoTaskType.TaskTypeKey);
        snapshot.ContentSchemaVersion.ShouldBe(1);
        snapshot.Title.ShouldBe("Fake snapshot task");
        snapshot.Summary.ShouldBe("A made-up summary.");
        snapshot.CandidateContent.Deserialize<TestEchoCandidateContent>(JsonSerializerOptions.Web).ShouldBe(new TestEchoCandidateContent("Fake phrase to echo"));
        snapshot.StaffContent.Deserialize<TestEchoStaffContent>(JsonSerializerOptions.Web).ShouldBe(new TestEchoStaffContent("fake-echo"));
        snapshot.ReferenceAnswer.ShouldBe("Echo the fake phrase.");
        snapshot.InterviewerNotes.ShouldBe("Ask about fake echoes.");
        snapshot.Rubric.Criteria.Count.ShouldBe(2);
        snapshot.Rubric.Criteria[0].ShouldBeOfType<ScaleCriterion>().ShouldSatisfyAllConditions(
            criterion => criterion.Weight.ShouldBe(4),
            criterion => criterion.Levels.ShouldNotBeNull().Single().ShouldBe(new ScaleLevel(4, "Crystal fake.")));
        snapshot.Rubric.Criteria[1].ShouldBe(rubric[1]);
    }

    [Fact]
    public async Task A_version_stored_with_another_content_schema_version_fails_loudly()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value));
        await Host.ExecuteSqlAsync($"UPDATE taskbank.task_versions SET content_schema_version = 2 WHERE task_id = {taskId.Value}");

        // Act
        var find = () => FindAsAsync(manager, taskId);

        // Assert
        (await Should.ThrowAsync<InvalidOperationException>(find)).Message.ShouldContain("content schema version 2");
    }

    [Fact]
    public async Task A_task_whose_type_is_no_longer_registered_fails_loudly()
    {
        // Arrange
        var manager = await CreateManagerAsync();
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value));
        await Host.ExecuteSqlAsync($"UPDATE taskbank.tasks SET task_type_key = 'retired-fake-type' WHERE id = {taskId.Value}");

        // Act
        var find = () => FindAsAsync(manager, taskId);

        // Assert
        (await Should.ThrowAsync<InvalidOperationException>(find)).Message.ShouldContain("no longer registered");
    }

    private async Task<TestUser> CreateManagerAsync() =>
        await Host.CreateUserAsync(UserBuilder.Manager((await Host.CreateTeamAsync()).Id));

    private async Task<TaskVersionSnapshot?> FindAsAsync(TestUser user, TaskId taskId)
    {
        await using var scope = Host.CreateScopeAs(user);
        return await scope.Services.GetRequiredService<IActiveTaskVersionQuery>().FindAsync(taskId, CancellationToken);
    }
}
