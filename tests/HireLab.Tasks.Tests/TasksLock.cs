using System.Text;
using System.Text.Json;
using HireLab.Modules.TaskBank.Seeds;

namespace HireLab.Tasks.Tests;

/// <summary>
/// <c>src/Tasks/tasks.lock.json</c> (ADR 0030): the <see cref="SeedPackageHash"/> of every published seed package, by
/// package folder (<c>&lt;slug&gt;/v&lt;N&gt;</c>), in ordinal order.
/// </summary>
internal sealed class TasksLock
{
    public const string FileName = "tasks.lock.json";

    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true, NewLine = "\n" };

    public TasksLock(IEnumerable<KeyValuePair<string, string>> versions)
    {
        Versions = new SortedDictionary<string, string>(versions.ToDictionary(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> Versions { get; }

    public static TasksLock Of(IEnumerable<SeedTaskPackage> packages) =>
        new(packages.Select(package => KeyValuePair.Create(package.Name, SeedPackageHash.Compute(package.Files))));

    public static TasksLock Load(string path) =>
        new(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(path))
            ?? throw new InvalidDataException($"{path} must be a JSON object."));

    public void Save(string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(Versions, _writeOptions) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    /// <summary>Compares this lock with the <paramref name="current"/> seed packages.</summary>
    public TasksLockDifference CompareWith(TasksLock current)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new TasksLockDifference(
            Changed: [.. Versions.Keys.Where(name => current.Versions.TryGetValue(name, out var hash) && hash != Versions[name])],
            Removed: [.. Versions.Keys.Where(name => !current.Versions.ContainsKey(name))],
            Unlocked: [.. current.Versions.Keys.Where(name => !Versions.ContainsKey(name))]);
    }

    /// <summary>This lock plus the <paramref name="current"/> versions it doesn't hold yet; locked hashes never change.</summary>
    public TasksLock WithNewVersionsOf(TasksLock current)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new TasksLock(Versions.Concat(current.Versions.Where(version => !Versions.ContainsKey(version.Key))));
    }
}

/// <summary>How the seed packages differ from <c>tasks.lock.json</c>, by package folder.</summary>
internal sealed record TasksLockDifference(IReadOnlyList<string> Changed, IReadOnlyList<string> Removed, IReadOnlyList<string> Unlocked)
{
    public bool IsEmpty => Changed.Count == 0 && Removed.Count == 0 && Unlocked.Count == 0;

    public override string ToString()
    {
        var problems = new List<string>();
        if (Changed.Count > 0)
        {
            problems.Add($"Changed after they were locked: {string.Join(", ", Changed)}. Published seed versions are immutable; restore them and add a new version instead.");
        }

        if (Removed.Count > 0)
        {
            problems.Add($"Locked but missing: {string.Join(", ", Removed)}. Restore them; a published seed version is never removed.");
        }

        if (Unlocked.Count > 0)
        {
            problems.Add($"Not in the lock yet: {string.Join(", ", Unlocked)}. Run eng/update-tasks-lock.ps1 and commit {TasksLock.FileName}.");
        }

        return string.Join(Environment.NewLine, problems);
    }
}
