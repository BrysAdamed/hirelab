using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Seeds;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.Tasks.Tests;

/// <summary>
/// The CI guard of the seed packages (ADR 0030): reads every package, validates it through the registered task types and
/// compares the packages with <c>tasks.lock.json</c>. With <c>update</c> it first adds the versions the lock doesn't hold
/// yet, never changing a locked one.
/// </summary>
internal static class TasksLockGuard
{
    public const string UpdateVariable = "HIRELAB_UPDATE_TASKS_LOCK";

    public static bool UpdateRequested => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    /// <summary>Returns what is still wrong. Throws <see cref="SeedPackageException"/> for a package that can't be read or is invalid.</summary>
    public static TasksLockDifference Check(ISeedPackageSource source, ITaskTypeRegistry taskTypes, string lockPath, bool update)
    {
        ArgumentNullException.ThrowIfNull(source);

        var packages = SeedPackageReader.Read(source.ReadFiles());
        var validator = new SeedPackageValidator(taskTypes, new TaskVersionValidator(taskTypes));
        foreach (var package in packages)
        {
            validator.Validate(package);
        }

        var current = TasksLock.Of(packages);
        var locked = File.Exists(lockPath) ? TasksLock.Load(lockPath) : new TasksLock([]);
        if (update)
        {
            locked = locked.WithNewVersionsOf(current);
            locked.Save(lockPath);
        }

        return locked.CompareWith(current);
    }
}
