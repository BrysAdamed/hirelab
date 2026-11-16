using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.Modules.TaskBank.Tasks;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using HireLab.TaskTypes.Abstractions;
using Microsoft.AspNetCore.Routing;
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
        builder.Services.AddTaskTypeRegistry();
        builder.Services.AddSingleton<TaskVersionValidator>();
        builder.Services.AddScoped<IActiveTaskVersionQuery, ActiveTaskVersionQuery>();
        builder.Services.AddSingleton<BankTaskTypes>();
    }

    /// <inheritdoc />
    public void MapStaffEndpoints(IEndpointRouteBuilder staff) => staff.MapTaskEndpoints();

    /// <inheritdoc />
    public void AddMigrationServices(IServiceCollection services) =>
        services.AddModuleDbContext<TaskBankDbContext>();

    /// <inheritdoc />
    public Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<TaskBankDbContext>().Database.MigrateAsync(cancellationToken);
}