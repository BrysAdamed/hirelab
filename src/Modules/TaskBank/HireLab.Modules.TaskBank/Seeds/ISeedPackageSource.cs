namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>Where seed package files come from: the <c>HireLab.Tasks</c> assembly in the migration worker, fixtures in tests.</summary>
internal interface ISeedPackageSource
{
    /// <summary>Every seed package file, with its path below <c>src/Tasks</c>, for example <c>fake-task/v1/task.json</c>.</summary>
    IReadOnlyList<SeedPackageFile> ReadFiles();
}

/// <summary>One file of a seed package. <see cref="Path"/> uses <c>/</c> and starts with the package's slug.</summary>
internal sealed record SeedPackageFile(string Path, byte[] Content);
