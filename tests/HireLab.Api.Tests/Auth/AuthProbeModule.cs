using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace HireLab.Api.Tests.Auth;

internal sealed class AuthProbeModule : IModule
{
    public const string StaffPath = "/api/staff/__probe";
    public const string CandidatePath = "/api/candidate/__probe";
    public const string CurrentUserPath = "/api/staff/__probe/current-user";
    public const string PoliciesPath = "/api/staff/__probe/policies";

    private const string ProbePattern = "/__probe";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
        staff.MapGet(ProbePattern, () => TypedResults.NoContent());
        staff.MapGet($"{ProbePattern}/current-user", (ICurrentUser user) =>
            TypedResults.Ok(new CurrentUserProbe(user.IsAuthenticated, user.Id.Value, user.Role, user.TeamId?.Value)));

        var policies = staff.MapGroup($"{ProbePattern}/policies");
        policies.MapGet($"/{StaffPolicies.Admin}", () => TypedResults.NoContent()).RequireAuthorization(StaffPolicies.Admin);
        policies.MapGet($"/{StaffPolicies.Manager}", () => TypedResults.NoContent()).RequireAuthorization(StaffPolicies.Manager);
        policies.MapGet($"/{StaffPolicies.ManagerOrInterviewer}", () => TypedResults.NoContent())
            .RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
    }

    public void MapCandidateEndpoints(IEndpointRouteBuilder candidate) => candidate.MapGet(ProbePattern, () => TypedResults.NoContent());
}

internal sealed record CurrentUserProbe(bool IsAuthenticated, Guid Id, StaffRole Role, Guid? TeamId);
