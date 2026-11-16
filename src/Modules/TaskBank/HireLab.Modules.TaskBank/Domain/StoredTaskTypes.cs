using HireLab.Modules.TaskBank.Contracts;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// Matches a stored task version with its registered task type (ADR 0028). MVP 1 has no content upgrade hook, so a
/// version stored with another content schema version than the type's fails loudly instead of being read wrongly.
/// </summary>
internal static class StoredTaskTypes
{
    /// <summary>
    /// The registered type of a stored version, or <c>null</c> when <paramref name="taskTypeKey"/> is no longer
    /// registered: the task bank leaves such a task out with a warning. Throws when the type is registered with another
    /// content schema version than <paramref name="contentSchemaVersion"/>.
    /// </summary>
    public static ITaskType? FindForStoredVersion(
        this ITaskTypeRegistry taskTypes,
        string taskTypeKey,
        int contentSchemaVersion,
        TaskVersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(taskTypes);

        var taskType = taskTypes.Find(taskTypeKey);
        if (taskType is not null && taskType.ContentSchemaVersion != contentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Task version {versionId} was stored with content schema version {contentSchemaVersion} of task type '{taskTypeKey}', "
                + $"but the registered type has content schema version {taskType.ContentSchemaVersion}. Content upgrades aren't supported (ADR 0028).");
        }

        return taskType;
    }
}
