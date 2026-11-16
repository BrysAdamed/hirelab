using HireLab.Modules.Sessions.CandidateAccess;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Queue;
using HireLab.Modules.Sessions.Sessions;
using HireLab.Modules.Sessions.UnfinishedSessions;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using HireLab.TaskTypes.Abstractions;
using Microsoft.AspNetCore.Routing;
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
        builder.Services.AddTaskTypeRegistry();
        builder.Services.AddScoped<IUnfinishedSessionsQuery, UnfinishedSessionsQuery>();
        builder.Services.AddScoped<LazyRepinning>();
        builder.Services.AddScoped<CandidateSessions>();
        builder.Services.AddScoped<ICandidateAccessValidator, CandidateAccessValidator>();
        builder.Services.AddScoped<IStaffSessionAccess, StaffSessionAccess>();
    }

    /// <inheritdoc />
    public void MapStaffEndpoints(IEndpointRouteBuilder staff) => staff.MapSessionEndpoints();

    /// <inheritdoc />
    public void MapCandidateEndpoints(IEndpointRouteBuilder candidate) => candidate.MapCandidateAccessEndpoints();

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<SessionsDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<SessionsDbContext>().Database.MigrateAsync(cancellationToken);
}
