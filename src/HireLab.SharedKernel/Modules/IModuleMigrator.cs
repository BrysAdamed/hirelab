using Microsoft.Extensions.DependencyInjection;

namespace HireLab.SharedKernel.Modules;

/// <summary>
/// A module's database entry point for <c>HireLab.MigrationService</c> (ADR 0004), implemented by the module's
/// registration entry point next to <see cref="IModule"/>, so the module's <c>DbContext</c> stays internal.
/// </summary>
public interface IModuleMigrator
{
    /// <summary>Registers only what migrating and seeding need, typically the module's <c>DbContext</c>.</summary>
    void AddMigrationServices(IServiceCollection services);

    /// <summary>Applies the module's pending migrations. Runs in its own service scope.</summary>
    Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Seeds obviously fake Development data after every module is migrated. Runs only in Development, in its
    /// own service scope, and must be idempotent.
    /// </summary>
    Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Creates what an environment needs before anyone can use it, for example the first admin's invite. Runs in every
    /// environment after every module is migrated (and, in Development, seeded), in its own service scope, and must be
    /// idempotent.
    /// </summary>
    Task BootstrapAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;
}
