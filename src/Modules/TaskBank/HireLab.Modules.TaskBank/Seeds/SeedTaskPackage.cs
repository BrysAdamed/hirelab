using HireLab.Modules.TaskBank.Domain;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>
/// One seed package read by <see cref="SeedPackageReader"/>: version <see cref="Number"/> of the system task
/// <see cref="Slug"/>, with its <c>task.json</c> mapped to task version details and every content file inlined. It still
/// has to pass <see cref="SeedPackageValidator"/>. <see cref="Files"/> are the package's files, with paths relative to
/// its folder.
/// </summary>
internal sealed record SeedTaskPackage(
    string Slug,
    int Number,
    string TaskTypeKey,
    TaskVersionDetails Details,
    IReadOnlyList<SeedPackageFile> Files)
{
    /// <summary>The package's folder below <c>src/Tasks</c>, for example <c>fake-task/v2</c>, which names it in errors and logs.</summary>
    public string Name => SeedPackageReader.FolderOf(Slug, Number);
}
