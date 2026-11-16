using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Tests.JobPositions;

internal static class JobPositionApi
{
    public static readonly Uri Positions = new("/api/staff/positions", UriKind.Relative);

    public static Uri Position(JobPositionId positionId) => new($"/api/staff/positions/{positionId}", UriKind.Relative);

    public static Uri Position(Guid positionId) => Position(new JobPositionId(positionId));

    public static Uri Close(JobPositionId positionId) => new($"/api/staff/positions/{positionId}/close", UriKind.Relative);

    public static Task<HttpResponseMessage> CloseAsync(this BrowserSession browser, JobPositionId positionId, uint version) =>
        browser.PostAsJsonAsync(Close(positionId), new { version });

    public static Uri PositionsIn(JobPositionState state) => new($"/api/staff/positions?state={state}", UriKind.Relative);

    public static JobPositionInput ValidInput(string title = "Fake Backend Developer") => new(
        title,
        "Builds **made-up** services.",
        "- Three years of fake experience",
        "- Knows imaginary frameworks",
        "Ask about the fake project.");

    public static Task<HttpResponseMessage> CreateAsync(this BrowserSession browser, JobPositionInput input) =>
        browser.PostAsJsonAsync(Positions, input);

    public static Task<HttpResponseMessage> EditAsync(this BrowserSession browser, JobPositionId positionId, JobPositionInput input, uint version) =>
        browser.PutAsJsonAsync(Position(positionId), new
        {
            input.Title,
            input.Description,
            input.Requirements,
            input.NiceToHaves,
            input.InterviewerNotes,
            version,
        });

    public static async Task<JobPositionId> CreatedAsync(this BrowserSession browser, JobPositionInput input)
    {
        using var response = await browser.CreateAsync(input);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<CreatedView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        response.Headers.Location.ShouldBe(Position(created.Id));
        return new JobPositionId(created.Id);
    }

    public static async Task<JobPositionView> PositionAsync(this BrowserSession browser, JobPositionId positionId)
    {
        using var response = await browser.GetAsync(Position(positionId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JobPositionView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<List<JobPositionSummaryView>> PositionsAsync(this BrowserSession browser, Uri? uri = null)
    {
        using var response = await browser.GetAsync(uri ?? Positions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<JobPositionSummaryView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

internal sealed record JobPositionInput(
    string? Title,
    string? Description,
    string? Requirements,
    string? NiceToHaves,
    string? InterviewerNotes);

internal sealed record CreatedView(Guid Id);

internal sealed record JobPositionView(
    Guid Id,
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes,
    JobPositionState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    Guid? ClosedById,
    string? ClosedByName,
    uint Version);

internal sealed record JobPositionSummaryView(Guid Id, string Title, JobPositionState State, DateTimeOffset CreatedAt);
