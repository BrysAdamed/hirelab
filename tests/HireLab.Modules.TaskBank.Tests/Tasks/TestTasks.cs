using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions;
using HireLab.Testing.TaskTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.TaskBank.Tests.Tasks;

/// <summary>Creates <c>test-echo</c> tasks through the domain, validated as the module validates them, and persists them.</summary>
internal static class TestTasks
{
    public static readonly DateTimeOffset PublishedAt = HireLabTestHost.StartTime;

    public static ITaskTypeRegistry TaskTypes { get; } = new ServiceCollection()
        .AddTaskType<TestEchoTaskType>()
        .BuildServiceProvider()
        .GetRequiredService<ITaskTypeRegistry>();

    public static TaskVersionValidator Validator { get; } = new(TaskTypes);

    public static ValidatedTaskVersion Valid(TaskVersionBuilder? builder = null) =>
        Validator.Validate(TestEchoTaskType.TaskTypeKey, (builder ?? new TaskVersionBuilder()).Build()).Value;

    /// <summary>A system task with one published version per builder.</summary>
    public static InterviewTask System(string slug, params TaskVersionBuilder[] versions)
    {
        var task = InterviewTask.CreateSystem(slug, Valid(versions.FirstOrDefault()), PublishedAt);
        foreach (var version in versions.Skip(1))
        {
            task.AddSystemVersion(Valid(version), PublishedAt);
        }

        return task;
    }

    /// <summary>A team task with <paramref name="publishedVersions"/> published versions and, optionally, a draft after them.</summary>
    public static InterviewTask Team(
        TeamId teamId,
        TaskVisibility visibility = TaskVisibility.Team,
        int publishedVersions = 1,
        bool withDraft = false)
    {
        var author = UserId.New();
        var task = InterviewTask.CreateForTeam(teamId, visibility, Valid(), author);
        for (var published = 0; published < publishedVersions; published++)
        {
            if (published > 0)
            {
                task.StartDraft(Valid(), author).IsSuccess.ShouldBeTrue();
            }

            task.PublishDraft(makeActive: true, author, PublishedAt).IsSuccess.ShouldBeTrue();
        }

        if (withDraft && publishedVersions > 0)
        {
            task.StartDraft(Valid(), author).IsSuccess.ShouldBeTrue();
        }

        return task;
    }

    public static async Task<TaskId> AddTaskAsync(this HireLabTestHost host, InterviewTask task)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return task.Id;
    }

    /// <summary>Changes a task through the domain as another request would, bypassing the visibility filter.</summary>
    public static async Task ChangeTaskAsync(this HireLabTestHost host, TaskId taskId, Action<InterviewTask> change)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        var task = await dbContext.Tasks.IgnoreQueryFilters()
            .Include(task => task.Versions)
            .SingleAsync(task => task.Id == taskId, cancellationToken);

        change(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Runs SQL against the task bank schema, for stored data the domain can't produce.</summary>
    public static async Task ExecuteSqlAsync(this HireLabTestHost host, FormattableString sql)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        await dbContext.Database.ExecuteSqlAsync(sql, TestContext.Current.CancellationToken);
    }
}
