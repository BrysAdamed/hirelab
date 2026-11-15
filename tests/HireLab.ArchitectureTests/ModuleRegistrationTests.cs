using HireLab.Api.Modules;
using HireLab.MigrationService;

namespace HireLab.ArchitectureTests;

public sealed class ModuleRegistrationTests
{
    [Fact]
    public void Api_registers_every_module()
    {
        // Act
        var registered = ModuleCatalog.All.Select(module => module.GetType().FullName);

        // Assert
        registered.ShouldBe(EntryPoints(), ignoreOrder: true);
    }

    [Fact]
    public void Migration_worker_migrates_every_module()
    {
        // Act
        var migrated = ModuleMigrators.All.Select(migrator => migrator.GetType().FullName);

        // Assert
        migrated.ShouldBe(EntryPoints(), ignoreOrder: true);
    }

    private static List<string> EntryPoints() => ModuleProject.All.Select(module => module.EntryPointTypeName).ToList();
}
