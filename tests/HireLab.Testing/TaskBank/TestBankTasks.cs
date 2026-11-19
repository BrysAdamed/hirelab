using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.Testing.TaskTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HireLab.Testing.TaskBank;

/// <summary>
/// Puts tasks with obviously fake values into the task bank through the TaskBank domain and its validation, for test
/// assemblies of modules that only hold a <see cref="TaskId"/>. Tasks are <see cref="TestEchoTaskType"/> ones unless a
/// task type and its content are given.
/// </summary>
public static class TestBankTasks
{
    private static int _sequence;

    /// <summary>Adds a task owned by <paramref name="teamId"/> with one published version titled <paramref name="title"/>.</summary>
    public static Task<TaskId> AddBankTaskAsync(this HireLabTestHost host, TeamId teamId, string? title = null, bool global = false) =>
        host.AddBankTaskAsync(teamId, global, services => Validate(services, title));

    /// <summary>
    /// Adds a team task of the shipped task type <paramref name="taskTypeKey"/> with one published version holding
    /// <paramref name="candidateContent"/> and <paramref name="staffContent"/>, serialised as the task type reads them.
    /// </summary>
    public static Task<TaskId> AddBankTaskAsync<TCandidateContent, TStaffContent>(
        this HireLabTestHost host,
        TeamId teamId,
        string taskTypeKey,
        TCandidateContent candidateContent,
        TStaffContent staffContent) =>
        host.AddBankTaskAsync(
            teamId,
            global: false,
            services => Validate(
                services,
                title: null,
                taskTypeKey,
                JsonSerializer.SerializeToElement(candidateContent, TaskTypeJson.Options),
                JsonSerializer.SerializeToElement(staffContent, TaskTypeJson.Options)));

    private static async Task<TaskId> AddBankTaskAsync(
        this HireLabTestHost host,
        TeamId teamId,
        bool global,
        Func<IServiceProvider, ValidatedTaskVersion> validate)
    {
        ArgumentNullException.ThrowIfNull(host);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var author = UserId.New();
        var task = InterviewTask.CreateForTeam(
            teamId,
            global ? TaskVisibility.Global : TaskVisibility.Team,
            validate(scope.ServiceProvider),
            author);
        task.PublishDraft(makeActive: true, author, host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue();

        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return task.Id;
    }

    /// <summary>Publishes a new version of the task titled <paramref name="title"/> and makes it active; returns its id.</summary>
    public static Task<TaskVersionId> PublishNewVersionAsync(this HireLabTestHost host, TaskId taskId, string title)
    {
        ArgumentNullException.ThrowIfNull(host);
        return host.ChangeBankTaskAsync(taskId, (task, services) =>
        {
            var author = UserId.New();
            task.StartDraft(Validate(services, title), author).IsSuccess.ShouldBeTrue();
            return task.PublishDraft(makeActive: true, author, host.Time.GetUtcNow()).Value.Id;
        });
    }

    /// <summary>Archives the task, which takes it out of the task bank.</summary>
    public static async Task ArchiveBankTaskAsync(this HireLabTestHost host, TaskId taskId)
    {
        ArgumentNullException.ThrowIfNull(host);
        var archived = await host.ChangeBankTaskAsync(taskId, (task, _) => task.Archive(UserId.New(), host.Time.GetUtcNow()).IsSuccess);
        archived.ShouldBeTrue();
    }

    private static async Task<TResult> ChangeBankTaskAsync<TResult>(
        this HireLabTestHost host,
        TaskId taskId,
        Func<InterviewTask, IServiceProvider, TResult> change)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskBankDbContext>();
        var task = await dbContext.Tasks.IgnoreQueryFilters()
            .Include(task => task.Versions)
            .SingleAsync(task => task.Id == taskId, cancellationToken);

        var result = change(task, scope.ServiceProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static ValidatedTaskVersion Validate(IServiceProvider services, string? title)
    {
        var number = Interlocked.Increment(ref _sequence);
        return Validate(
            services,
            title ?? $"Fake Bank Task {number}",
            TestEchoTaskType.TaskTypeKey,
            JsonSerializer.SerializeToElement(new TestEchoCandidateContent($"Fake phrase {number}"), JsonSerializerOptions.Web),
            JsonSerializer.SerializeToElement(new TestEchoStaffContent("fake-echo"), JsonSerializerOptions.Web));
    }

    private static ValidatedTaskVersion Validate(
        IServiceProvider services,
        string? title,
        string taskTypeKey,
        JsonElement candidateContent,
        JsonElement staffContent)
    {
        var details = new TaskVersionDetails(
            title ?? $"Fake Bank Task {Interlocked.Increment(ref _sequence)}",
            "A made-up task for tests.",
            TaskLevel.Mid,
            EstimatedMinutes: 30,
            ["fake"],
            "A made-up reference answer.",
            "Ask about fake echoes.",
            new Rubric(
            [
                new ScaleCriterion("fake-quality", "Fake quality", "How convincingly fake it is.", Weight: 2),
                new CheckCriterion("fake-echo", "Fake echo", "Did the candidate echo the phrase?", CheckAnswer.Yes, Critical: true),
            ]),
            candidateContent,
            staffContent);

        var validated = services.GetRequiredService<TaskVersionValidator>().Validate(taskTypeKey, details);
        validated.IsSuccess.ShouldBeTrue();
        return validated.Value;
    }
}
