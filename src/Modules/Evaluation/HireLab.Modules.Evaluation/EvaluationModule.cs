using HireLab.Modules.Evaluation.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.Evaluation;

/// <summary>The Evaluation module's registration entry point.</summary>
public sealed class EvaluationModule : IModule, IModuleMigrator
{
    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<EvaluationDbContext>();
        builder.Services.AddHandlers<EvaluationDbContext>(typeof(EvaluationModule).Assembly);
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<EvaluationDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<EvaluationDbContext>().Database.MigrateAsync(cancellationToken);
}