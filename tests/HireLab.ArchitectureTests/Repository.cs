namespace HireLab.ArchitectureTests;

internal static class Repository
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HireLab.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"HireLab.slnx was not found above {AppContext.BaseDirectory}.");
    }
}
