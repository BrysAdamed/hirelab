using HireLab.Modules.Identity.Auth;
using HireLab.Modules.Identity.Auth.Login;
using HireLab.Modules.Identity.Authentication;
using HireLab.Modules.Identity.Bootstrap;
using HireLab.Modules.Identity.Invites;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.ResetLinks;
using HireLab.Modules.Identity.Seed;
using HireLab.Modules.Identity.Teams;
using HireLab.Modules.Identity.Users;
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
    /// <summary>
    /// The setting that makes one migration run issue a fresh admin link (ADR 0022): an invite replacing a pending one, or
    /// a reset link when the admin exists.
    /// </summary>
    public const string IssueAdminLinkSetting = AdminBootstrapOptions.Section + ":" + nameof(AdminBootstrapOptions.IssueNewLink);

    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<IdentityModuleDbContext>();
        builder.Services.AddHandlers<IdentityModuleDbContext>(typeof(IdentityModule).Assembly);
        builder.Services.AddStaffIdentityCore().AddStaffSignIn();
        builder.Services.AddScoped<OneTimeLinkIssuer>();
        builder.Services.AddScoped<InviteIssuer>();
        builder.Services.AddScoped<ResetLinkIssuer>();
        builder.Services.AddSingleton<UnknownUserPasswordCheck>();
    }

    /// <inheritdoc />
    public void MapStaffEndpoints(IEndpointRouteBuilder staff)
    {
        staff.MapAuthEndpoints();
        staff.MapInviteEndpoints();
        staff.MapResetLinkEndpoints();
        staff.MapTeamEndpoints();
        staff.MapUserEndpoints();
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>();
        services.AddStaffIdentityCore();
        services.AddScoped<OneTimeLinkIssuer>();
        services.AddScoped<InviteIssuer>();
        services.AddScoped<ResetLinkIssuer>();
        services.AddAdminBootstrap();
    }

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IdentityModuleDbContext>().Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    public Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        ActivatorUtilities.CreateInstance<DevelopmentSeed>(services).RunAsync(cancellationToken);

    /// <inheritdoc />
    public Task BootstrapAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<AdminBootstrap>().RunAsync(cancellationToken);
}
