using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace HireLab.SharedKernel.Modules;

/// <summary>
/// A module's public registration entry point, the only public type in a module project (ADR 0001).
/// The API host adds the module's services, then maps its endpoints into the route groups, which already
/// carry the group's authorisation policy and OpenAPI document.
/// </summary>
public interface IModule
{
    /// <summary>Registers the module's services, typically its DbContext and <c>AddHandlers</c>.</summary>
    void AddServices(IHostApplicationBuilder builder);

    /// <summary>Maps the module's staff endpoints into the <c>/api/staff</c> group.</summary>
    void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
    }

    /// <summary>Maps the module's candidate endpoints into the <c>/api/candidate</c> group.</summary>
    void MapCandidateEndpoints(IEndpointRouteBuilder candidate)
    {
    }
}
