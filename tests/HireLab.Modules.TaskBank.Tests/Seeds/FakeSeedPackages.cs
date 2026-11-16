using System.Text;
using HireLab.Modules.TaskBank.Seeds;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.TaskBank.Tests.Seeds;

/// <summary>An in-memory seed package source made of <see cref="TestEchoSeedPackage"/>s and loose files.</summary>
internal sealed class FakeSeedPackages : ISeedPackageSource
{
    private readonly List<SeedPackageFile> _files = [];

    public FakeSeedPackages(params TestEchoSeedPackage[] packages)
    {
        foreach (var package in packages)
        {
            Add(package);
        }
    }

    public FakeSeedPackages Add(TestEchoSeedPackage package)
    {
        _files.AddRange(package.Files().Select(file => new SeedPackageFile(file.Key, file.Value)));
        return this;
    }

    /// <summary>Replaces every file of the package's folder with the package's files.</summary>
    public FakeSeedPackages Replace(TestEchoSeedPackage package)
    {
        _files.RemoveAll(file => file.Path.StartsWith($"{package.Folder}/", StringComparison.Ordinal));
        return Add(package);
    }

    public FakeSeedPackages AddFile(string path, string text) => AddFile(path, Encoding.UTF8.GetBytes(text));

    public FakeSeedPackages AddFile(string path, byte[] content)
    {
        _files.Add(new SeedPackageFile(path, content));
        return this;
    }

    public IReadOnlyList<SeedPackageFile> ReadFiles() => [.. _files];

    public IReadOnlyList<SeedTaskPackage> Read() => SeedPackageReader.Read(ReadFiles());
}
