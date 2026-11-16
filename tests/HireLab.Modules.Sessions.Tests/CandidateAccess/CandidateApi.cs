using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

internal static class CandidateApi
{
    public static readonly Uri Interview = new("/api/candidate/session", UriKind.Relative);

    public static async Task<InterviewView> InterviewAsync(this BrowserSession candidate)
    {
        using var response = await candidate.GetAsync(Interview);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<InterviewView>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }
}

internal sealed record InterviewView(string Stage, CurrentTaskView? CurrentTask);

internal sealed record CurrentTaskView(Guid Id, string TaskType, JsonElement Content);
