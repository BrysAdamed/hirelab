using System.Reflection;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.Modules.TaskBank.Seeds;
using HireLab.Modules.TaskBank.Tasks;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using HireLab.TaskTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HireLab.Modules.TaskBank;

/// <summary>The TaskBank module's registration entry point.</summary>
public sealed class TaskBankModule : IModule, IModuleMigrator
{
    private readonly ISeedPackageSource _seedPackages;

    /// <summary>Creates the module without seed packages, as the API uses it.</summary>
    public TaskBankModule()
    {
        _seedPackages = new NoSeedPackages();
    }

    /// <summary>
    /// Creates the module for the migration worker, which imports the seed packages (ADR 0030) embedded in
    /// <paramref name="seedPackages"/> as resources named <paramref name="resourcePrefix"/> plus their path below
    /// <c>src/Tasks</c>.
    /// </summary>
    public TaskBankModule(Assembly seedPackages, string resourcePrefix)
    {
        ArgumentNullException.ThrowIfNull(seedPackages);
        ArgumentException.ThrowIfNullOrEmpty(resourcePrefix);

        _seedPackages = new EmbeddedSeedPackageSource(seedPackages, resourcePrefix);
    }

    /// <inheritdoc />
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddModuleDbContext<TaskBankDbContext>();
        builder.Services.AddHandlers<TaskBankDbContext>(typeof(TaskBankModule).Assembly);
        builder.Services.AddTaskTypeRegistry();
        builder.Services.AddSingleton<TaskVersionValidator>();
        builder.Services.AddScoped<IActiveTaskVersionQuery, ActiveTaskVersionQuery>();
    }

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services)
    {
        services.AddModuleDbContext<TaskBankDbContext>();
        services.AddSeedImport(_seedPackages);
    }

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<TaskBankDbContext>().Database.MigrateAsync(cancellationToken);

    /// <inheritdoc />
    public Task BootstrapAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<SeedTaskImport>().RunAsync(cancellationToken);
}
