using System.Reflection;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>Reads the seed package files embedded in an assembly as resources named <c>resourcePrefix</c> plus their path.</summary>
internal sealed class EmbeddedSeedPackageSource(Assembly assembly, string resourcePrefix) : ISeedPackageSource
{
    public IReadOnlyList<SeedPackageFile> ReadFiles() =>
        assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .Select(name => new SeedPackageFile(name[resourcePrefix.Length..].Replace('\\', '/'), Read(name)))
            .ToList();

    private byte[] Read(string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{resourceName}' was not found.");
        using var content = new MemoryStream();
        stream.CopyTo(content);
        return content.ToArray();
    }
}
