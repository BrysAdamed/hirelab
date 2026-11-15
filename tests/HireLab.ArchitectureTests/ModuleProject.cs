using System.Reflection;
using System.Xml.Linq;

namespace HireLab.ArchitectureTests;

internal sealed record ModuleProject(string Name, string ProjectPath, string ContractsProjectPath)
{
    public const string ProjectPrefix = "HireLab.Modules.";
    public const string ContractsSuffix = ".Contracts";

    public static IReadOnlyList<ModuleProject> All { get; } = Discover();

    public string AssemblyName => ProjectPrefix + Name;

    public string ContractsAssemblyName => AssemblyName + ContractsSuffix;

    public string EntryPointTypeName => $"{AssemblyName}.{Name}Module";

    public string MigrationsNamespace => $"{AssemblyName}.Persistence.Migrations";

    public Assembly LoadAssembly() => Assembly.Load(AssemblyName);

    public Assembly LoadContractsAssembly() => Assembly.Load(ContractsAssemblyName);

    public IEnumerable<string> ProjectFiles() => [ProjectPath, ContractsProjectPath];

    public static bool IsModuleImplementation(string assemblyOrProjectName) =>
        assemblyOrProjectName.StartsWith(ProjectPrefix, StringComparison.Ordinal)
        && !assemblyOrProjectName.EndsWith(ContractsSuffix, StringComparison.Ordinal);

    public static IReadOnlyList<string> ProjectReferencesOf(string projectPath) =>
        XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")?.Value ?? string.Empty))
            .ToList();

    private static List<ModuleProject> Discover()
    {
        var modulesDirectory = Path.Combine(Repository.Root, "src", "Modules");

        return Directory.GetDirectories(modulesDirectory)
            .Select(directory => Path.GetFileName(directory))
            .Select(name => new ModuleProject(
                name,
                Path.Combine(modulesDirectory, name, ProjectPrefix + name, $"{ProjectPrefix}{name}.csproj"),
                Path.Combine(modulesDirectory, name, ProjectPrefix + name + ContractsSuffix, $"{ProjectPrefix}{name}{ContractsSuffix}.csproj")))
            .OrderBy(module => module.Name, StringComparer.Ordinal)
            .ToList();
    }
}
