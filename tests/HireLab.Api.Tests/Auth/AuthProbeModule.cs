using HireLab.SharedKernel.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace HireLab.Api.Tests.Auth;

internal sealed class AuthProbeModule : IModule
{
    public const string StaffPath = "/api/staff/__probe";
    public const string CandidatePath = "/api/candidate/__probe";

    private const string ProbePattern = "/__probe";

    public void AddServices(IHostApplicationBuilder builder)
    {
    }

    public void MapStaffEndpoints(IEndpointRouteBuilder staff) => staff.MapGet(ProbePattern, () => TypedResults.NoContent());

    public void MapCandidateEndpoints(IEndpointRouteBuilder candidate) => candidate.MapGet(ProbePattern, () => TypedResults.NoContent());
}
