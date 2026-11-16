using HireLab.Modules.TaskBank.Seeds;
using HireLab.Testing.TaskTypes;

namespace HireLab.Tasks.Tests;

/// <summary><see cref="TestEchoSeedPackage"/>s as a seed package source, in place of the embedded packages.</summary>
internal sealed class FixturePackages(params TestEchoSeedPackage[] packages) : ISeedPackageSource
{
    public IReadOnlyList<SeedPackageFile> ReadFiles() =>
        [.. packages.SelectMany(package => package.Files()).Select(file => new SeedPackageFile(file.Key, file.Value))];
}
