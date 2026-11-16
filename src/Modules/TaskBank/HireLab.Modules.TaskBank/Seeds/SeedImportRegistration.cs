using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireLab.Modules.TaskBank.Seeds;

internal static class SeedImportRegistration
{
    /// <summary>Registers seed import from <paramref name="source"/>. The host registers the task types.</summary>
    public static IServiceCollection AddSeedImport(this IServiceCollection services, ISeedPackageSource source)
    {
        services.AddOptions<SeedImportOptions>().BindConfiguration(SeedImportOptions.Section);
        services.AddTaskTypeRegistry();
        services.TryAddSingleton<TaskVersionValidator>();
        services.TryAddSingleton<SeedPackageValidator>();
        services.TryAddSingleton(source);
        services.AddScoped<SeedTaskImport>();
        return services;
    }
}
