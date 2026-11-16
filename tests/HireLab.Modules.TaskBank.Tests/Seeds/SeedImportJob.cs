using HireLab.Modules.TaskBank.Seeds;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Catalog;
using HireLab.Testing.TaskTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace HireLab.Modules.TaskBank.Tests.Seeds;

/// <summary>
/// The TaskBank's bootstrap step as the migration worker runs it: <see cref="TaskBankModule.AddMigrationServices"/> on the
/// test database and the test host's clock, with the shipped task types and <see cref="TestEchoTaskType"/> registered and
/// the given packages.
/// </summary>
internal static class SeedImportJob
{
    public static async Task RunAsync(HireLabTestHost host, ISeedPackageSource packages, bool importSeedTasks = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SeedImportOptions.Section}:{nameof(SeedImportOptions.ImportSeedTasks)}"] = importSeedTasks ? "true" : "false",
            })
            .Build());
        services.AddSingleton<TimeProvider>(host.Time);
        services.AddSingleton(_ => NpgsqlDataSource.Create(host.DatabaseConnectionString));
        services.AddLogging();

        var module = new TaskBankModule();
        module.AddMigrationServices(services);
        services.Replace(ServiceDescriptor.Singleton(packages));
        services.AddTaskTypes();
        services.AddTaskType<TestEchoTaskType>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await module.BootstrapAsync(scope.ServiceProvider, TestContext.Current.CancellationToken);
    }
}
