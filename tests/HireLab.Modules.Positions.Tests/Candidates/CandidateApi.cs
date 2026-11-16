using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;

namespace HireLab.Modules.Positions.Tests.Candidates;

internal static class CandidateApi
{
    public static Uri CandidatesOf(JobPositionId positionId) => new($"/api/staff/positions/{positionId}/candidates", UriKind.Relative);

    public static Uri Candidate(CandidateId candidateId) => new($"/api/staff/candidates/{candidateId}", UriKind.Relative);

    public static CandidateInput ValidInput(string fullName = "Jane Fakeperson") => new(
        fullName,
        "jane.fakeperson@example.com",
        "+00 123 456 789",
        ["https://profiles.example.com/jane-fakeperson", "https://code.example.org/jfake"]);

    public static Task<HttpResponseMessage> AddAsync(this BrowserSession browser, JobPositionId positionId, CandidateInput input) =>
        browser.PostAsJsonAsync(CandidatesOf(positionId), input);

    public static Task<HttpResponseMessage> EditAsync(this BrowserSession browser, CandidateId candidateId, CandidateInput input, uint version) =>
        browser.PutAsJsonAsync(Candidate(candidateId), new
        {
            input.FullName,
            input.Email,
            input.Phone,
            input.Links,
            version,
        });

    public static async Task<AddedCandidateView> AddedAsync(this BrowserSession browser, JobPositionId positionId, CandidateInput input)
    {
        using var response = await browser.AddAsync(positionId, input);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var added = (await response.Content.ReadFromJsonAsync<AddedCandidateView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        response.Headers.Location.ShouldBe(Candidate(new CandidateId(added.Id)));
        return added;
    }

    public static async Task<CandidateView> CandidateAsync(this BrowserSession browser, CandidateId candidateId)
    {
        using var response = await browser.GetAsync(Candidate(candidateId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CandidateView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static async Task<List<PositionCandidateView>> CandidatesAsync(this BrowserSession browser, JobPositionId positionId)
    {
        using var response = await browser.GetAsync(CandidatesOf(positionId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<PositionCandidateView>>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

internal sealed record CandidateInput(string? FullName, string? Email, string? Phone, List<string>? Links);

internal sealed record AddedCandidateView(Guid Id, int Number)
{
    public CandidateId CandidateId => new(Id);
}

internal sealed record CandidateView(
    Guid Id,
    Guid PositionId,
    string PositionTitle,
    JobPositionState PositionState,
    int Number,
    string DisplayName,
    string? FullName,
    string? Email,
    string? Phone,
    List<string> Links,
    bool IsAnonymised,
    DateTimeOffset CreatedAt,
    uint Version);

internal sealed record PositionCandidateView(
    Guid Id,
    int Number,
    string DisplayName,
    string? Email,
    string? Phone,
    bool IsAnonymised,
    DateTimeOffset CreatedAt);
