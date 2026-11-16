using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.TaskBank.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Sessions.Tests.Sessions;

internal static class SessionApi
{
    public static readonly Uri TeamSessions = new("/api/staff/sessions", UriKind.Relative);

    public static Uri SessionsOf(CandidateId candidateId) => new($"/api/staff/candidates/{candidateId}/sessions", UriKind.Relative);

    public static Uri Session(InterviewSessionId sessionId) => new($"/api/staff/sessions/{sessionId}", UriKind.Relative);

    public static Uri Schedule(InterviewSessionId sessionId) => new($"/api/staff/sessions/{sessionId}/schedule", UriKind.Relative);

    public static Uri Queue(InterviewSessionId sessionId) => new($"/api/staff/sessions/{sessionId}/tasks", UriKind.Relative);

    public static Uri QueuedTask(InterviewSessionId sessionId, Guid sessionTaskId) =>
        new($"/api/staff/sessions/{sessionId}/tasks/{sessionTaskId}", UriKind.Relative);

    public static Uri Order(InterviewSessionId sessionId) => new($"/api/staff/sessions/{sessionId}/tasks/order", UriKind.Relative);

    public static Task<HttpResponseMessage> CreateAsync(this BrowserSession browser, CandidateId candidateId, DateTimeOffset? scheduledAt = null) =>
        browser.PostAsJsonAsync(SessionsOf(candidateId), new { scheduledAt });

    public static async Task<CreatedSessionView> CreatedAsync(this BrowserSession browser, CandidateId candidateId, DateTimeOffset? scheduledAt = null)
    {
        using var response = await browser.CreateAsync(candidateId, scheduledAt);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<CreatedSessionView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        response.Headers.Location.ShouldBe(Session(created.SessionId));
        return created;
    }

    public static async Task<SessionView> SessionAsync(this BrowserSession browser, InterviewSessionId sessionId)
    {
        using var response = await browser.GetAsync(Session(sessionId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SessionView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<List<CandidateSessionView>> CandidateSessionsAsync(this BrowserSession browser, CandidateId candidateId)
    {
        using var response = await browser.GetAsync(SessionsOf(candidateId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<CandidateSessionView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<List<TeamSessionView>> TeamSessionsAsync(this BrowserSession browser)
    {
        using var response = await browser.GetAsync(TeamSessions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<TeamSessionView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> AddTaskAsync(this BrowserSession browser, InterviewSessionId sessionId, TaskId taskId) =>
        browser.PostAsJsonAsync(Queue(sessionId), new { taskId = taskId.Value });

    public static async Task<Guid> AddedTaskAsync(this BrowserSession browser, InterviewSessionId sessionId, TaskId taskId)
    {
        using var response = await browser.AddTaskAsync(sessionId, taskId);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedView>(TestContext.Current.CancellationToken)).ShouldNotBeNull().Id;
    }

    public static Task<HttpResponseMessage> ReorderAsync(this BrowserSession browser, InterviewSessionId sessionId, params Guid[] sessionTaskIds) =>
        browser.PutAsJsonAsync(Order(sessionId), new { sessionTaskIds });

    /// <summary>Puts a session in a state the API can't reach yet, as running and finishing a session will.</summary>
    public static async Task SetStateAsync(this HireLabTestHost host, InterviewSessionId sessionId, InterviewSessionState state)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SessionsDbContext>();
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE sessions.interview_sessions SET state = {state.ToString()} WHERE id = {sessionId.Value}",
            TestContext.Current.CancellationToken);
    }

    /// <summary>Runs SQL against the sessions schema, for stored data the API can't produce yet.</summary>
    public static async Task ExecuteSessionsSqlAsync(this HireLabTestHost host, FormattableString sql)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SessionsDbContext>();
        await dbContext.Database.ExecuteSqlAsync(sql, TestContext.Current.CancellationToken);
    }

    /// <summary>Reads a session task as stored, snapshot included, bypassing the team filter.</summary>
    public static async Task<SessionTask> StoredTaskAsync(this HireLabTestHost host, Guid sessionTaskId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SessionsDbContext>();
        return await dbContext.Set<SessionTask>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(task => task.Id == new SessionTaskId(sessionTaskId), TestContext.Current.CancellationToken);
    }
}

internal sealed record CreatedView(Guid Id);

internal sealed record CreatedSessionView(Guid Id, int Number)
{
    public InterviewSessionId SessionId => new(Id);
}

internal sealed record SessionView(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    Guid CandidateId,
    string CandidateDisplayName,
    Guid CreatedById,
    string? CreatedByName,
    DateTimeOffset CreatedAt,
    int QueueRevision,
    List<SessionTaskView> Tasks);

internal sealed record SessionTaskView(
    Guid Id,
    Guid TaskId,
    SessionTaskState State,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary);

internal sealed record CandidateSessionView(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    int TaskCount,
    DateTimeOffset CreatedAt);

internal sealed record TeamSessionView(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    int TaskCount,
    Guid CandidateId,
    string CandidateDisplayName);
