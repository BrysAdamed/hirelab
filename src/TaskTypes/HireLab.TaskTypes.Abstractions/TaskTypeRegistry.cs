using System.Text.RegularExpressions;

namespace HireLab.TaskTypes.Abstractions;

internal sealed partial class TaskTypeRegistry : ITaskTypeRegistry
{
    private readonly Dictionary<string, ITaskType> _byKey;

    public TaskTypeRegistry(IEnumerable<ITaskType> taskTypes)
    {
        var all = taskTypes.ToList();
        foreach (var taskType in all)
        {
            EnsureWellFormed(taskType);
        }

        var duplicates = all.GroupBy(taskType => taskType.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"'{group.Key}' ({string.Join(", ", group.Select(taskType => taskType.GetType().FullName))})")
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Task type keys must be unique. Registered more than once: {string.Join("; ", duplicates)}.");
        }

        All = all.OrderBy(taskType => taskType.Key, StringComparer.Ordinal).ToList();
        _byKey = all.ToDictionary(taskType => taskType.Key, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITaskType> All { get; }

    public ITaskType? Find(string key) => _byKey.GetValueOrDefault(key);

    private static void EnsureWellFormed(ITaskType taskType)
    {
        if (taskType.Key is null || !KeyPattern().IsMatch(taskType.Key))
        {
            throw new InvalidOperationException(
                $"Task type {taskType.GetType().FullName} has the key '{taskType.Key}'. A key must be kebab-case, for example 'code-review'.");
        }

        if (taskType.ContentSchemaVersion < 1 || taskType.ResponseSchemaVersion < 1)
        {
            throw new InvalidOperationException($"Task type '{taskType.Key}' must have content and response schema versions of at least 1.");
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
