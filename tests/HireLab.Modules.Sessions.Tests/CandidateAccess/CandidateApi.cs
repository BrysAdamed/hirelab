using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

internal static class CandidateApi
{
    public static readonly Uri Interview = new("/api/candidate/session", UriKind.Relative);

    public static readonly Uri Response = new("/api/candidate/session/response", UriKind.Relative);

    public static async Task<InterviewView> InterviewAsync(this BrowserSession candidate)
    {
        using var response = await candidate.GetAsync(Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<InterviewView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    public static Task<HttpResponseMessage> SaveResponseAsync(this BrowserSession candidate, Guid sessionTaskId, object response, int revision) =>
        candidate.PutAsJsonAsync(Response, new { sessionTaskId, response, revision });

    public static async Task<int> SavedResponseAsync(this BrowserSession candidate, Guid sessionTaskId, object response, int revision)
    {
        using var saved = await candidate.SaveResponseAsync(sessionTaskId, response, revision);
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await saved.Content.ReadFromJsonAsync<SavedResponseView>(TestContext.Current.CancellationToken)).ShouldNotBeNull().Revision;
    }
}

internal sealed record InterviewView(string Stage, CurrentTaskView? CurrentTask);

internal sealed record CurrentTaskView(Guid Id, string TaskType, JsonElement Content, JsonElement? Response, int Revision);

internal sealed record SavedResponseView(int Revision);
