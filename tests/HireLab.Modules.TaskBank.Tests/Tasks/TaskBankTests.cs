using System.Net;
using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.TaskBank.Tests.Tasks;

public sealed class TaskBankTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Staff_of_a_team_list_view_and_tag_their_teams_tasks(StaffRole role)
    {
        // Arrange
        var staff = await SignInAsAsync(role);
        var taskId = await AddTeamTaskAsync(
            staff.User.TeamId!.Value,
            new TaskVersionBuilder()
                .WithTitle("Fake echo task")
                .WithSummary("Echo a made-up phrase.")
                .WithLevel(TaskLevel.Junior)
                .WithEstimatedMinutes(15)
                .WithTags("fake", "echo"));
        using var browser = await CreateBrowserAsync(staff);

        // Act
        var tasks = await browser.TasksAsync();
        var task = await browser.TaskAsync(taskId);
        var tags = await browser.StringsAsync(TaskBankApi.Tags);
        var taskTypes = await browser.StringsAsync(TaskBankApi.TaskTypes);

        // Assert
        var listed = tasks.ShouldHaveSingleItem();
        listed.Tags.ShouldBe(["fake", "echo"]);
        listed.ShouldBe(new TaskSummaryView(
            taskId.Value,
            1,
            TestEchoTaskType.TaskTypeKey,
            "Fake echo task",
            "Echo a made-up phrase.",
            TaskLevel.Junior,
            15,
            listed.Tags,
            BankVisibility.Team));
        task.Title.ShouldBe("Fake echo task");
        tags.ShouldBe(["echo", "fake"]);
        taskTypes.ShouldBe([TestEchoTaskType.TaskTypeKey]);
    }

    [Fact]
    public async Task The_detail_holds_the_active_versions_reference_answer_notes_rubric_and_both_content_parts()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var rubric = new RubricCriterion[]
        {
            new ScaleCriterion("fake-clarity", "Fake clarity", "How clear the fake echo is.", Weight: 3, [new ScaleLevel(4, "Crystal fake.")]),
            new CheckCriterion("fake-echo", "Fake echo", "Echoed the phrase?", CheckAnswer.Yes, Critical: true),
        };
        var taskId = await AddTeamTaskAsync(
            manager.User.TeamId!.Value,
            new TaskVersionBuilder()
                .WithPhrase("Fake phrase to echo")
                .WithReferenceAnswer("Echo the **fake** phrase.")
                .WithInterviewerNotes("Ask about fake echoes.")
                .WithRubric(rubric));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var task = await browser.TaskAsync(taskId);

        // Assert
        task.VersionNumber.ShouldBe(1);
        task.ReferenceAnswer.ShouldBe("Echo the **fake** phrase.");
        task.InterviewerNotes.ShouldBe("Ask about fake echoes.");
        task.Rubric.Criteria.Count.ShouldBe(2);
        task.Rubric.Criteria[0].ShouldBeOfType<ScaleCriterion>().ShouldSatisfyAllConditions(
            criterion => criterion.Weight.ShouldBe(3),
            criterion => criterion.Levels.ShouldNotBeNull().Single().ShouldBe(new ScaleLevel(4, "Crystal fake.")));
        task.Rubric.Criteria[1].ShouldBe(rubric[1]);
        task.CandidateContent.Deserialize<TestEchoCandidateContent>(JsonSerializerOptions.Web).ShouldBe(new TestEchoCandidateContent("Fake phrase to echo"));
        task.StaffContent.Deserialize<TestEchoStaffContent>(JsonSerializerOptions.Web).ShouldBe(new TestEchoStaffContent("fake-echo"));
    }

    [Fact]
    public async Task The_admin_is_refused()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var taskId = await AddTeamTaskAsync(team.Id, new TaskVersionBuilder(), TaskVisibility.Global);
        using var admin = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Admin));

        // Act
        using var list = await admin.GetAsync(TaskBankApi.Tasks);
        using var detail = await admin.GetAsync(TaskBankApi.Task(taskId));
        using var tags = await admin.GetAsync(TaskBankApi.Tags);
        using var taskTypes = await admin.GetAsync(TaskBankApi.TaskTypes);

        // Assert
        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        detail.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        tags.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        taskTypes.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_bank_holds_the_own_teams_tasks_and_every_global_and_system_task_with_their_badges()
    {
        // Arrange
        var interviewer = await SignInAsAsync(StaffRole.Interviewer);
        var otherTeam = await Host.CreateTeamAsync();
        var own = await AddTeamTaskAsync(interviewer.User.TeamId!.Value, new TaskVersionBuilder().WithTitle("Fake A own"));
        var global = await AddTeamTaskAsync(otherTeam.Id, new TaskVersionBuilder().WithTitle("Fake B global"), TaskVisibility.Global);
        var system = await Host.AddTaskAsync(TestTasks.System("fake-system-task", new TaskVersionBuilder().WithTitle("Fake C system")));
        await AddTeamTaskAsync(otherTeam.Id, new TaskVersionBuilder().WithTitle("Fake D hidden").WithTags("other-team-only"));
        using var browser = await CreateBrowserAsync(interviewer);

        // Act
        var tasks = await browser.TasksAsync();
        var tags = await browser.StringsAsync(TaskBankApi.Tags);

        // Assert
        tasks.Select(task => (task.Id, task.Visibility)).ShouldBe(
        [
            (own.Value, BankVisibility.Team),
            (global.Value, BankVisibility.Global),
            (system.Value, BankVisibility.System),
        ]);
        tags.ShouldNotContain("other-team-only");
    }

    [Fact]
    public async Task Another_teams_team_task_is_not_found()
    {
        // Arrange
        var otherTeam = await Host.CreateTeamAsync();
        var taskId = await AddTeamTaskAsync(otherTeam.Id, new TaskVersionBuilder());
        using var manager = await CreateBrowserAsync(await SignInAsAsync(StaffRole.Manager));

        // Act
        using var response = await manager.GetAsync(TaskBankApi.Task(taskId));

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Task.NotFound");
    }

    [Fact]
    public async Task An_archived_task_is_left_out()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var taskId = await AddTeamTaskAsync(manager.User.TeamId!.Value, new TaskVersionBuilder().WithTags("archived-only"));
        await Host.ChangeTaskAsync(taskId, task => task.Archive(manager.User.Id, Host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue());
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync();
        var tags = await browser.StringsAsync(TaskBankApi.Tags);
        using var detail = await browser.GetAsync(TaskBankApi.Task(taskId));

        // Assert
        tasks.ShouldBeEmpty();
        tags.ShouldBeEmpty();
        await detail.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Task.NotFound");
    }

    [Fact]
    public async Task A_task_whose_type_is_no_longer_registered_is_left_out()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        var retired = await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake retired").WithTags("retired-only"));
        var kept = await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake kept").WithTags("kept"));
        await Host.ExecuteSqlAsync($"UPDATE taskbank.tasks SET task_type_key = 'retired-fake-type' WHERE id = {retired.Value}");
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync();
        var tags = await browser.StringsAsync(TaskBankApi.Tags);
        var taskTypes = await browser.StringsAsync(TaskBankApi.TaskTypes);
        using var detail = await browser.GetAsync(TaskBankApi.Task(retired));

        // Assert
        tasks.ShouldHaveSingleItem().Id.ShouldBe(kept.Value);
        tags.ShouldBe(["kept"]);
        taskTypes.ShouldBe([TestEchoTaskType.TaskTypeKey]);
        await detail.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Task.NotFound");
    }

    [Fact]
    public async Task A_task_shows_its_active_version_and_one_with_only_a_draft_is_left_out()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        var versioned = await Host.AddTaskAsync(TestTasks.Team(teamId, publishedVersions: 2, withDraft: true));
        await Host.AddTaskAsync(TestTasks.Team(teamId, publishedVersions: 0));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync();

        // Assert
        var task = tasks.ShouldHaveSingleItem();
        task.Id.ShouldBe(versioned.Value);
        task.VersionNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Tasks_are_listed_by_title()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake Zebra"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake Alpaca"));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync();

        // Assert
        tasks.Select(task => task.Title).ShouldBe(["Fake Alpaca", "Fake Zebra"]);
    }

    [Fact]
    public async Task The_tag_filter_keeps_tasks_with_every_given_tag()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake both").WithTags("csharp", "cqrs", "fake"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake one").WithTags("csharp"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake none").WithTags("security"));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var both = await browser.TasksAsync(TaskBankApi.TasksWhere("tag=csharp&tag=CQRS"));
        var one = await browser.TasksAsync(TaskBankApi.TasksWhere("tag=csharp"));

        // Assert
        both.Select(task => task.Title).ShouldBe(["Fake both"]);
        one.Select(task => task.Title).ShouldBe(["Fake both", "Fake one"]);
    }

    [Fact]
    public async Task The_level_filter_keeps_tasks_of_any_given_level()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake junior").WithLevel(TaskLevel.Junior));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake mid").WithLevel(TaskLevel.Mid));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake senior").WithLevel(TaskLevel.Senior));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync(TaskBankApi.TasksWhere("level=Junior&level=Senior"));

        // Assert
        tasks.Select(task => task.Title).ShouldBe(["Fake junior", "Fake senior"]);
    }

    [Fact]
    public async Task The_task_type_filter_keeps_tasks_of_that_type()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        await AddTeamTaskAsync(manager.User.TeamId!.Value, new TaskVersionBuilder());
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var echo = await browser.TasksAsync(TaskBankApi.TasksWhere($"taskType={TestEchoTaskType.TaskTypeKey}"));
        var other = await browser.TasksAsync(TaskBankApi.TasksWhere("taskType=fake-other-type"));

        // Assert
        echo.ShouldHaveSingleItem();
        other.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_search_matches_the_title_or_summary_ignoring_case()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake Parrot echo").WithSummary("A made-up task."));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake summary match").WithSummary("Talk like a parrot."));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake unrelated").WithSummary("Nothing to see."));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync(TaskBankApi.TasksWhere("search=%20PARROT%20"));

        // Assert
        tasks.Select(task => task.Title).ShouldBe(["Fake Parrot echo", "Fake summary match"]);
    }

    [Fact]
    public async Task The_search_treats_wildcards_as_text()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake 100% coverage"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake 1000 coverage"));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync(TaskBankApi.TasksWhere("search=100%25"));

        // Assert
        tasks.Select(task => task.Title).ShouldBe(["Fake 100% coverage"]);
    }

    [Fact]
    public async Task Filters_combine()
    {
        // Arrange
        var manager = await SignInAsAsync(StaffRole.Manager);
        var teamId = manager.User.TeamId!.Value;
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake match").WithLevel(TaskLevel.Senior).WithTags("csharp"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake wrong level").WithLevel(TaskLevel.Mid).WithTags("csharp"));
        await AddTeamTaskAsync(teamId, new TaskVersionBuilder().WithTitle("Fake wrong tag").WithLevel(TaskLevel.Senior).WithTags("sql"));
        using var browser = await CreateBrowserAsync(manager);

        // Act
        var tasks = await browser.TasksAsync(TaskBankApi.TasksWhere($"tag=csharp&level=Senior&taskType={TestEchoTaskType.TaskTypeKey}&search=fake"));

        // Assert
        tasks.Select(task => task.Title).ShouldBe(["Fake match"]);
    }

    private async Task<TaskId> AddTeamTaskAsync(TeamId teamId, TaskVersionBuilder details, TaskVisibility visibility = TaskVisibility.Team)
    {
        var author = UserId.New();
        var task = InterviewTask.CreateForTeam(teamId, visibility, TestTasks.Valid(details), author);
        task.PublishDraft(makeActive: true, author, TestTasks.PublishedAt).IsSuccess.ShouldBeTrue();
        return await Host.AddTaskAsync(task);
    }
}
