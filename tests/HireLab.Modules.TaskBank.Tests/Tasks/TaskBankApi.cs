using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Tests.Tasks;

internal static class TaskBankApi
{
    public static readonly Uri Tasks = new("/api/staff/tasks", UriKind.Relative);

    public static readonly Uri Tags = new("/api/staff/tasks/tags", UriKind.Relative);

    public static readonly Uri TaskTypes = new("/api/staff/tasks/types", UriKind.Relative);

    public static Uri Task(TaskId taskId) => new($"/api/staff/tasks/{taskId}", UriKind.Relative);

    public static Uri TasksWhere(string query) => new($"/api/staff/tasks?{query}", UriKind.Relative);

    public static async Task<List<TaskSummaryView>> TasksAsync(this BrowserSession browser, Uri? uri = null)
    {
        using var response = await browser.GetAsync(uri ?? Tasks);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<TaskSummaryView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<TaskView> TaskAsync(this BrowserSession browser, TaskId taskId)
    {
        using var response = await browser.GetAsync(Task(taskId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TaskView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<List<string>> StringsAsync(this BrowserSession browser, Uri uri)
    {
        using var response = await browser.GetAsync(uri);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<string>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

internal sealed record TaskSummaryView(
    Guid Id,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary,
    TaskLevel Level,
    int EstimatedMinutes,
    IReadOnlyList<string> Tags,
    BankVisibility Visibility);

internal sealed record TaskView(
    Guid Id,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary,
    TaskLevel Level,
    int EstimatedMinutes,
    IReadOnlyList<string> Tags,
    BankVisibility Visibility,
    string ReferenceAnswer,
    string? InterviewerNotes,
    Rubric Rubric,
    JsonElement CandidateContent,
    JsonElement StaffContent);
