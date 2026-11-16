using System.Reflection;

namespace HireLab.ArchitectureTests;

public sealed class TaskTypeBoundaryTests
{
    private const string AbstractionsName = "HireLab.TaskTypes.Abstractions";

    public static TheoryData<string> TaskTypeProjects => new(Directory
        .GetFiles(Path.Combine(Repository.Root, "src", "TaskTypes"), "*.csproj", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(Repository.Root, path)));

    [Fact]
    public void Abstractions_reference_no_module()
    {
        // Act
        var moduleReferences = Assembly.Load(AbstractionsName).GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith(ModuleProject.ProjectPrefix, StringComparison.Ordinal));

        // Assert
        moduleReferences.ShouldBeEmpty("Modules and task type projects depend on the task type contract, never the other way round (ADR 0028)");
    }

    [Theory]
    [MemberData(nameof(TaskTypeProjects))]
    public void Task_type_projects_reference_modules_only_through_contracts(string project)
    {
        // Act
        var violations = ModuleProject.ProjectReferencesOf(Path.Combine(Repository.Root, project))
            .Where(ModuleProject.IsModuleImplementation);

        // Assert
        violations.ShouldBeEmpty("A task type changes module data only through a module's .Contracts project (ADR 0013)");
    }
}
