using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.TaskBank;

/// <summary>The TaskBank module's registration entry point.</summary>
public sealed class TaskBankModule : IModule, IModuleMigrator
{
    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<TaskBankDbContext>();
        builder.Services.AddHandlers<TaskBankDbContext>(typeof(TaskBankModule).Assembly);
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<TaskBankDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<TaskBankDbContext>().Database.MigrateAsync(cancellationToken);
}