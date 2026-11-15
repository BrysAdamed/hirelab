using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
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
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<IdentityModuleDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IdentityModuleDbContext>().Database.MigrateAsync(cancellationToken);
}