using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.Modules.TaskBank.Persistence.Configurations;
using HireLab.Modules.TaskBank.Tests.Tasks;
using HireLab.SharedKernel.Persistence;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.Testing.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.TaskBank.Tests.Persistence;

public sealed class TaskStorageTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task A_task_version_is_stored_and_read_back_whole()
    {
        // Arrange
        var details = new TaskVersionBuilder()
            .WithTags("csharp", "role:qa", "c#")
            .WithRubric(
                new ScaleCriterion("fake-quality", "Fake quality", "How fake it is.", Weight: 5),
                new CheckCriterion("fake-echo", "Fake echo", "Echoed?", CheckAnswer.No, Critical: true));
        var validated = TestTasks.Valid(details);
        var taskId = await Host.AddTaskAsync(TestTasks.System("fake-stored-task", details));

        // Act
        await using var scope = Host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        var task = await dbContext.Tasks.IgnoreQueryFilters().Include(task => task.Versions).SingleAsync(task => task.Id == taskId, CancellationToken);

        // Assert
        task.Slug.ShouldBe("fake-stored-task");
        task.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
        var version = task.Versions.ShouldHaveSingleItem();
        version.Tags.ShouldBe(["csharp", "role:qa", "c#"]);
        version.Rubric.Criteria.ShouldBe(validated.Details.Rubric!.Criteria);
        version.CandidateContent.GetProperty("phrase").GetString().ShouldBe("Hello, fake world");
        version.StaffContent.GetProperty("matchCriterionId").GetString().ShouldBe("fake-echo");
        version.ContentSchemaVersion.ShouldBe(1);
        version.ContentHash.ShouldBe(validated.ContentHash);
        version.PublishedAt.ShouldBe(TestTasks.PublishedAt);
        version.CreatedAt.ShouldBe(Host.Time.GetUtcNow());
    }

    [Fact]
    public async Task Tasks_and_versions_are_filtered_by_visibility()
    {
        // Arrange
        var ownTeam = await Host.CreateTeamAsync();
        var otherTeam = await Host.CreateTeamAsync();
        var visible = new[]
        {
            await Host.AddTaskAsync(TestTasks.Team(ownTeam.Id, TaskVisibility.Team)),
            await Host.AddTaskAsync(TestTasks.Team(ownTeam.Id, TaskVisibility.Global)),
            await Host.AddTaskAsync(TestTasks.Team(otherTeam.Id, TaskVisibility.Global)),
            await Host.AddTaskAsync(TestTasks.System("fake-visible-task", new TaskVersionBuilder())),
        };
        await Host.AddTaskAsync(TestTasks.Team(otherTeam.Id, TaskVisibility.Team));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(ownTeam.Id));
        await using var scope = Host.CreateScopeAs(interviewer);
        var dbContext = scope.Services.GetRequiredService<TaskBankDbContext>();

        // Act
        var tasks = await dbContext.Tasks.Select(task => task.Id).ToListAsync(CancellationToken);
        var versions = await dbContext.TaskVersions.Select(version => version.TaskId).ToListAsync(CancellationToken);

        // Assert
        tasks.ShouldBe(visible, ignoreOrder: true);
        versions.ShouldBe(visible, ignoreOrder: true);
    }

    [Fact]
    public async Task The_admin_sees_no_tasks_or_versions()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        await Host.AddTaskAsync(TestTasks.Team(team.Id, TaskVisibility.Team));
        await Host.AddTaskAsync(TestTasks.Team(team.Id, TaskVisibility.Global));
        await Host.AddTaskAsync(TestTasks.System("fake-hidden-task", new TaskVersionBuilder()));
        var admin = await Host.CreateUserAsync(UserBuilder.Admin());
        await using var scope = Host.CreateScopeAs(admin);
        var dbContext = scope.Services.GetRequiredService<TaskBankDbContext>();

        // Act
        var tasks = await dbContext.Tasks.CountAsync(CancellationToken);
        var versions = await dbContext.TaskVersions.CountAsync(CancellationToken);
        var unfiltered = await dbContext.Tasks.IgnoreQueryFilters().CountAsync(CancellationToken);

        // Assert
        tasks.ShouldBe(0);
        versions.ShouldBe(0);
        unfiltered.ShouldBe(3);
    }

    [Fact]
    public async Task Tasks_in_the_bank_are_filtered_by_tag()
    {
        // Arrange
        var team = await Host.CreateTeamAsync();
        var tagged = await Host.AddTaskAsync(TestTasks.System("fake-tagged-task", new TaskVersionBuilder().WithTags("security", "csharp")));
        await Host.AddTaskAsync(TestTasks.System("fake-untagged-task", new TaskVersionBuilder().WithTags("csharp")));
        var interviewer = await Host.CreateUserAsync(UserBuilder.Interviewer(team.Id));
        await using var scope = Host.CreateScopeAs(interviewer);
        var dbContext = scope.Services.GetRequiredService<TaskBankDbContext>();

        // Act
        var found = await dbContext.TasksInBank()
            .Where(active => active.Version.Tags.Contains("security"))
            .Select(active => active.Task.Id)
            .ToListAsync(CancellationToken);

        // Assert
        found.ShouldBe([tagged]);
    }

    [Fact]
    public async Task Two_system_tasks_cannot_share_a_slug()
    {
        // Arrange
        await Host.AddTaskAsync(TestTasks.System("fake-duplicate-task", new TaskVersionBuilder()));

        // Act
        var add = () => Host.AddTaskAsync(TestTasks.System("fake-duplicate-task", new TaskVersionBuilder()));

        // Assert
        (await Should.ThrowAsync<DbUpdateException>(add)).ViolatedConstraint().ShouldBe(InterviewTaskConfiguration.SlugIndex);
    }

    [Fact]
    public async Task Two_concurrent_drafts_of_one_task_cannot_both_be_saved()
    {
        // Arrange
        var manager = await Host.CreateUserAsync(UserBuilder.Manager((await Host.CreateTeamAsync()).Id));
        var taskId = await Host.AddTaskAsync(TestTasks.Team(manager.TeamId!.Value));
        await using var firstScope = Host.CreateScopeAs(manager);
        await using var secondScope = Host.CreateScopeAs(manager);
        var first = firstScope.Services.GetRequiredService<TaskBankDbContext>();
        var second = secondScope.Services.GetRequiredService<TaskBankDbContext>();
        var firstTask = await first.Tasks.Include(task => task.Versions).SingleAsync(task => task.Id == taskId, CancellationToken);
        var secondTask = await second.Tasks.Include(task => task.Versions).SingleAsync(task => task.Id == taskId, CancellationToken);
        firstTask.StartDraft(TestTasks.Valid(), manager.Id).IsSuccess.ShouldBeTrue();
        secondTask.StartDraft(TestTasks.Valid(), manager.Id).IsSuccess.ShouldBeTrue();
        await first.SaveChangesAsync(CancellationToken);

        // Act
        var save = () => second.SaveChangesAsync(CancellationToken);

        // Assert
        (await Should.ThrowAsync<DbUpdateException>(save)).ViolatedConstraint().ShouldBeOneOf(
            TaskVersionConfiguration.DraftIndex,
            TaskVersionConfiguration.NumberIndex);
    }
}
