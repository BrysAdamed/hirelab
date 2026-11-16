namespace HireLab.Tasks.Tests;

internal static class Repository
{
    public static string Root { get; } = FindRoot();

    public static string TasksLockPath => Path.Combine(Root, "src", "Tasks", TasksLock.FileName);

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
