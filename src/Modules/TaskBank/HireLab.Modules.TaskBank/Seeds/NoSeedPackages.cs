namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>The seed package source of a host that doesn't ship the seed packages: there are none to import.</summary>
internal sealed class NoSeedPackages : ISeedPackageSource
{
    public IReadOnlyList<SeedPackageFile> ReadFiles() => [];
}
