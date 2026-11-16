using HireLab.Modules.Identity.Auth;
using HireLab.Modules.Identity.Authentication;
using HireLab.Modules.Identity.Invites;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Seed;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.Identity;

/// <summary>The Identity module's registration entry point.</summary>
public sealed class IdentityModule : IModule, IModuleMigrator
{
    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<IdentityModuleDbContext>();
        builder.Services.AddHandlers<IdentityModuleDbContext>(typeof(IdentityModule).Assembly);
        builder.Services.AddStaffIdentityCore().AddStaffSignIn();
        builder.Services.AddScoped<InviteIssuer>();
    }

    /// <inheritdoc />
    public void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
        staff.MapAuthEndpoints();
        staff.MapInviteEndpoints();
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>();
        services.AddStaffIdentityCore();
        services.AddScoped<InviteIssuer>();
    }

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IdentityModuleDbContext>().Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    public Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        ActivatorUtilities.CreateInstance<DevelopmentSeed>(services).RunAsync(cancellationToken);
}
