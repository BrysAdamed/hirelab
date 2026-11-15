using HireLab.SharedKernel.Modules;

namespace HireLab.ArchitectureTests;

public sealed class ModuleVisibilityTests
{
    public static TheoryData<string> Modules => ModuleBoundaryTests.Modules;

    [Theory]
    [MemberData(nameof(Modules))]
    public void Only_the_registration_entry_point_is_public(string name)
    {
        // Arrange
        var module = ModuleProject.All.Single(project => project.Name == name);

        // Act
        var publicTypes = module.LoadAssembly().GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith(module.MigrationsNamespace, StringComparison.Ordinal) != true)
            .Select(type => type.FullName);

        // Assert
        publicTypes.ShouldBe([module.EntryPointTypeName], "Everything in a module is internal except its registration entry point (ADR 0001)");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Registration_entry_point_is_a_module_and_a_migrator(string name)
    {
        // Arrange
        var module = ModuleProject.All.Single(project => project.Name == name);

        // Act
        var entryPoint = module.LoadAssembly().GetType(module.EntryPointTypeName, throwOnError: true)!;

        // Assert
        entryPoint.IsAssignableTo(typeof(IModule)).ShouldBeTrue();
        entryPoint.IsAssignableTo(typeof(IModuleMigrator)).ShouldBeTrue();
    }
}
