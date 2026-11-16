using System.Net;
using HireLab.SharedKernel.Users;

namespace HireLab.Api.Tests.Auth;

public sealed class AuthSweepTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly HttpStatusCode[] _rejected = [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden];

    private static readonly string[] _anonymousEndpoints =
    [
        "GET /api/staff/auth/antiforgery",
        "POST /api/staff/auth/login",
    ];

    [Theory]
    [InlineData(GroupEndpoint.StaffGroup, AuthProbeModule.StaffPath)]
    [InlineData(GroupEndpoint.CandidateGroup, AuthProbeModule.CandidatePath)]
    public void Sweep_covers_the_probe_endpoint_of_each_group(string group, string probePath)
    {
        // Act
        var endpoints = GroupEndpoint.InGroup(Host.Services, group);

        // Assert
        endpoints.ShouldContain(endpoint => endpoint.Uri.OriginalString == probePath);
    }

    [Fact]
    public async Task Probe_endpoints_accept_their_own_group_cookie()
    {
        // Arrange
        using var staffClient = Host.CreateClient((await SignInAsAsync(StaffRole.Interviewer)).Cookies);
        using var candidateClient = Host.CreateClient(await Host.SignInAsCandidateAsync());

        // Act
        using var staffResponse = await staffClient.GetAsync(new Uri(AuthProbeModule.StaffPath, UriKind.Relative), CancellationToken);
        using var candidateResponse = await candidateClient.GetAsync(new Uri(AuthProbeModule.CandidatePath, UriKind.Relative), CancellationToken);

        // Assert
        staffResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        candidateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public void Only_the_known_endpoints_allow_anonymous_requests()
    {
        // Act
        var anonymous = GroupEndpoint.InGroup(Host.Services, GroupEndpoint.StaffGroup)
            .Concat(GroupEndpoint.InGroup(Host.Services, GroupEndpoint.CandidateGroup))
            .Where(endpoint => endpoint.AllowsAnonymous)
            .Select(endpoint => endpoint.ToString());

        // Assert
        anonymous.ShouldBe(_anonymousEndpoints, ignoreOrder: true);
    }

    [Theory]
    [InlineData(GroupEndpoint.StaffGroup)]
    [InlineData(GroupEndpoint.CandidateGroup)]
    public async Task Every_endpoint_rejects_anonymous_requests_with_401(string group)
    {
        // Arrange
        using var client = Host.CreateClient();

        // Act
        var responses = await SendToEveryEndpointAsync(client, group);

        // Assert
        responses.Where(response => response.Status != HttpStatusCode.Unauthorized)
            .ShouldBeEmpty("These endpoints don't answer 401 to anonymous requests");
    }

    [Fact]
    public async Task Every_staff_endpoint_rejects_the_candidate_cookie()
    {
        // Arrange
        using var client = Host.CreateClient(await Host.SignInAsCandidateAsync());

        // Act
        var responses = await SendToEveryEndpointAsync(client, GroupEndpoint.StaffGroup);

        // Assert
        responses.Where(response => !_rejected.Contains(response.Status))
            .ShouldBeEmpty("These staff endpoints accept the candidate cookie");
    }

    [Fact]
    public async Task Every_candidate_endpoint_rejects_the_staff_cookie()
    {
        // Arrange
        using var client = Host.CreateClient((await SignInAsAsync(StaffRole.Interviewer)).Cookies);

        // Act
        var responses = await SendToEveryEndpointAsync(client, GroupEndpoint.CandidateGroup);

        // Assert
        responses.Where(response => !_rejected.Contains(response.Status))
            .ShouldBeEmpty("These candidate endpoints accept the staff cookie");
    }

    [Fact(Skip = "TODO: needs teams and staff users from HLB-3. Sweep every staff endpoint with a resource id of another team and expect 404.")]
    public void Every_staff_endpoint_hides_other_teams_resources()
    {
    }

    [Fact(Skip = "TODO: needs the admin role from HLB-3. Sweep every endpoint that returns candidate data as the admin and expect no candidate data.")]
    public void Admin_gets_no_candidate_data()
    {
    }

    private async Task<IReadOnlyList<SweepResponse>> SendToEveryEndpointAsync(HttpClient client, string group)
    {
        var endpoints = GroupEndpoint.InGroup(Host.Services, group).Where(endpoint => !endpoint.AllowsAnonymous).ToList();
        endpoints.ShouldNotBeEmpty();

        var responses = new List<SweepResponse>();
        foreach (var endpoint in endpoints)
        {
            using var request = endpoint.CreateRequest();
            using var response = await client.SendAsync(request, CancellationToken);
            responses.Add(new SweepResponse(endpoint, response.StatusCode));
        }

        return responses;
    }

    private sealed record SweepResponse(GroupEndpoint Endpoint, HttpStatusCode Status)
    {
        public override string ToString() => $"{Endpoint} -> {(int)Status}";
    }
}
