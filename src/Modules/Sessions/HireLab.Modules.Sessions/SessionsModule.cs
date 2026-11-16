using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.UnfinishedSessions;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.Sessions;

/// <summary>The Sessions module's registration entry point.</summary>
public sealed class SessionsModule : IModule, IModuleMigrator
{
    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<SessionsDbContext>();
        builder.Services.AddHandlers<SessionsDbContext>(typeof(SessionsModule).Assembly);
        builder.Services.AddScoped<IUnfinishedSessionsQuery, UnfinishedSessionsQuery>();
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<SessionsDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<SessionsDbContext>().Database.MigrateAsync(cancellationToken);
}