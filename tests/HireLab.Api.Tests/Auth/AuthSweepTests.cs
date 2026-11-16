using System.Net;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Api.Tests.Auth;

public sealed class AuthSweepTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly HttpStatusCode[] _rejected = [HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden];

    private static readonly HashSet<string?> _teamDataExceptions =
    [
        "HireLab.Modules.Identity.Domain.User",
        "HireLab.Modules.Identity.Domain.Invite",
    ];

    private static readonly Dictionary<string, string> _ownTeamFilters = new(StringComparer.Ordinal)
    {
        ["HireLab.Modules.TaskBank.Domain.InterviewTask"] = "Visibility",
        ["HireLab.Modules.TaskBank.Domain.TaskVersion"] = "Visibility",
    };

    private static readonly string[] _anonymousEndpoints =
    [
        "GET /api/staff/auth/antiforgery",
        "POST /api/staff/auth/login",
        "POST /api/staff/invites/preview",
        "POST /api/staff/invites/redeem",
        "POST /api/staff/reset-links/preview",
        "POST /api/staff/reset-links/redeem",
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

    [Fact]
    public void Every_entity_with_a_team_is_team_owned_and_filtered_by_team()
    {
        // Arrange
        using var scope = Host.Services.CreateScope();

        // Act
        var entityTypes = ModuleDbContextTypes()
            .SelectMany(type => ((DbContext)scope.ServiceProvider.GetRequiredService(type)).Model.GetEntityTypes())
            .ToList();
        var teamOwned = entityTypes.Where(entityType => entityType.ClrType.IsAssignableTo(typeof(ITeamOwned))).ToList();

        // Assert
        teamOwned.ShouldContain(entityType => entityType.ClrType == typeof(TeamNote));
        teamOwned.Where(entityType => entityType.GetRootType().FindDeclaredQueryFilter(TeamQueryFilter.Name) is null)
            .ShouldBeEmpty("These team-owned entities have no team query filter");
        entityTypes.Where(entityType => HasTeam(entityType)
                && !teamOwned.Contains(entityType)
                && !_teamDataExceptions.Contains(entityType.ClrType.FullName)
                && !_ownTeamFilters.ContainsKey(entityType.ClrType.FullName!))
            .Select(entityType => entityType.ClrType.FullName)
            .ShouldBeEmpty($"These entities store a team but aren't {nameof(ITeamOwned)}, so other teams and the admin would see them");
    }

    [Fact]
    public void Entities_with_their_own_team_filter_declare_it()
    {
        // Arrange
        using var scope = Host.Services.CreateScope();

        // Act
        var entityTypes = ModuleDbContextTypes()
            .SelectMany(type => ((DbContext)scope.ServiceProvider.GetRequiredService(type)).Model.GetEntityTypes())
            .Where(entityType => _ownTeamFilters.ContainsKey(entityType.ClrType.FullName!))
            .ToList();

        // Assert
        entityTypes.Select(entityType => entityType.ClrType.FullName).ShouldBe(_ownTeamFilters.Keys, ignoreOrder: true);
        entityTypes.Where(entityType => entityType.FindDeclaredQueryFilter(_ownTeamFilters[entityType.ClrType.FullName!]) is null)
            .Select(entityType => entityType.ClrType.FullName)
            .ShouldBeEmpty("These entities have no filter of their own, so every team and the admin would see them");
    }

    [Theory]
    [InlineData(StaffRole.Admin)]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Interviewer)]
    public async Task Every_endpoint_behind_a_role_policy_rejects_the_other_roles_with_403(StaffRole role)
    {
        // Arrange
        using var client = Host.CreateClient((await SignInAsAsync(role)).Cookies);
        var forbidden = new List<GroupEndpoint>();
        foreach (var endpoint in GroupEndpoint.InGroup(Host.Services, GroupEndpoint.StaffGroup))
        {
            if (await AllowedRolesAsync(endpoint) is { } allowed && !allowed.Contains(role.ToString()))
            {
                forbidden.Add(endpoint);
            }
        }

        // Act
        var responses = await SendAsync(client, forbidden);

        // Assert
        responses.Where(response => response.Status != HttpStatusCode.Forbidden)
            .ShouldBeEmpty($"These endpoints don't answer 403 to the {role}");
    }

    private IEnumerable<Type> ModuleDbContextTypes() =>
        Host.Services.GetServices<IModule>()
            .Select(module => module.GetType().Assembly)
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IModuleDbContext)));

    private static bool HasTeam(IEntityType entityType) =>
        entityType.GetProperties().Any(property => !property.IsPrimaryKey() && (property.ClrType == typeof(TeamId) || property.ClrType == typeof(TeamId?)));

    private async Task<IReadOnlySet<string>?> AllowedRolesAsync(GroupEndpoint endpoint)
    {
        var policyProvider = Host.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        HashSet<string>? allowed = null;
        foreach (var policyName in endpoint.Policies)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName)
                ?? throw new InvalidOperationException($"{endpoint} requires the unknown policy {policyName}.");
            foreach (var roles in policy.Requirements.OfType<RolesAuthorizationRequirement>())
            {
                allowed = allowed is null ? [.. roles.AllowedRoles] : [.. allowed.Intersect(roles.AllowedRoles)];
            }
        }

        return allowed;
    }

    private async Task<IReadOnlyList<SweepResponse>> SendToEveryEndpointAsync(HttpClient client, string group) =>
        await SendAsync(client, GroupEndpoint.InGroup(Host.Services, group).Where(endpoint => !endpoint.AllowsAnonymous).ToList());

    private static async Task<IReadOnlyList<SweepResponse>> SendAsync(HttpClient client, IReadOnlyList<GroupEndpoint> endpoints)
    {
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
