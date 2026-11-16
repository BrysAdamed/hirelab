using System.Net;
using System.Net.Http.Json;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireLab.Api.Tests.Auth;

public sealed class RolePolicyTests(ApiTestHost host) : IntegrationTest(host)
{
    [Theory]
    [InlineData(StaffPolicies.Admin, StaffRole.Admin)]
    [InlineData(StaffPolicies.Manager, StaffRole.Manager)]
    [InlineData(StaffPolicies.ManagerOrInterviewer, StaffRole.Manager)]
    [InlineData(StaffPolicies.ManagerOrInterviewer, StaffRole.Interviewer)]
    public async Task Policy_admits_its_roles(string policy, StaffRole role)
    {
        // Arrange
        using var client = CreateClient(await SignInAsAsync(role));

        // Act
        using var response = await client.GetAsync(PolicyUri(policy), CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(StaffPolicies.Admin, StaffRole.Manager)]
    [InlineData(StaffPolicies.Admin, StaffRole.Interviewer)]
    [InlineData(StaffPolicies.Manager, StaffRole.Admin)]
    [InlineData(StaffPolicies.Manager, StaffRole.Interviewer)]
    [InlineData(StaffPolicies.ManagerOrInterviewer, StaffRole.Admin)]
    public async Task Policy_rejects_other_roles_with_403_problem_details(string policy, StaffRole role)
    {
        // Arrange
        using var client = CreateClient(await SignInAsAsync(role));

        // Act
        using var response = await client.GetAsync(PolicyUri(policy), CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        problem.ShouldNotBeNull().Status.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Theory]
    [InlineData(StaffPolicies.Admin)]
    [InlineData(StaffPolicies.Manager)]
    [InlineData(StaffPolicies.ManagerOrInterviewer)]
    public async Task Policy_rejects_the_candidate_cookie_with_401(string policy)
    {
        // Arrange
        using var client = Host.CreateClient(await Host.SignInAsCandidateAsync());

        // Act
        using var response = await client.GetAsync(PolicyUri(policy), CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static Uri PolicyUri(string policy) => new($"{AuthProbeModule.PoliciesPath}/{policy}", UriKind.Relative);
}
