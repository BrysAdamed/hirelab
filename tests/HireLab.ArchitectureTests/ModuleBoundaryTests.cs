namespace HireLab.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
    public static TheoryData<string> Modules => new(ModuleProject.All.Select(module => module.Name));

    [Fact]
    public void Every_module_has_a_project_and_a_contracts_project()
    {
        // Act
        var modules = ModuleProject.All;

        // Assert
        modules.ShouldNotBeEmpty();
        modules.SelectMany(module => module.ProjectFiles()).Where(path => !File.Exists(path)).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_projects_reference_other_modules_only_through_contracts(string name)
    {
        // Arrange
        var module = Find(name);

        // Act
        var violations = module.ProjectFiles()
            .SelectMany(project => ModuleProject.ProjectReferencesOf(project)
                .Where(ModuleProject.IsModuleImplementation)
                .Select(reference => $"{Path.GetFileNameWithoutExtension(project)} -> {reference}"));

        // Assert
        violations.ShouldBeEmpty("A module may reference another module only through its .Contracts project (ADR 0001)");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_assemblies_reference_other_modules_only_through_contracts(string name)
    {
        // Arrange
        var module = Find(name);

        // Act
        var violations = ReferencesOf(module).Where(reference => ModuleProject.IsModuleImplementation(reference.Reference));

        // Assert
        violations.ShouldBeEmpty("A module may reference another module only through its .Contracts project (ADR 0001)");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Modules_do_not_reference_SignalR(string name)
    {
        // Arrange
        var module = Find(name);

        // Act
        var violations = ReferencesOf(module)
            .Where(reference => reference.Reference.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.Ordinal));

        // Assert
        violations.ShouldBeEmpty("Modules raise integration events and never reference SignalR (ADR 0006)");
    }

    private static ModuleProject Find(string name) => ModuleProject.All.Single(module => module.Name == name);

    private static IEnumerable<AssemblyReference> ReferencesOf(ModuleProject module) =>
        new[] { module.LoadAssembly(), module.LoadContractsAssembly() }
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Select(reference => new AssemblyReference(assembly.GetName().Name!, reference.Name!)));

    private sealed record AssemblyReference(string Assembly, string Reference)
    {
        public override string ToString() => $"{Assembly} -> {Reference}";
    }
}
