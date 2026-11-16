using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.TaskBank.Tasks;

/// <summary>
/// Decides which tasks the task bank shows by their task type (ADR 0028): a task whose type is no longer registered is
/// left out with a warning, and one stored with another content schema version than its type's fails loudly.
/// </summary>
internal sealed partial class BankTaskTypes(ITaskTypeRegistry taskTypes, ILogger<BankTaskTypes> logger)
{
    /// <summary>The keys of the registered task types.</summary>
    public IReadOnlyList<string> RegisteredKeys { get; } = [.. taskTypes.All.Select(taskType => taskType.Key)];

    public bool IsShown(TaskId taskId, string taskTypeKey, int contentSchemaVersion, TaskVersionId versionId)
    {
        if (taskTypes.FindForStoredVersion(taskTypeKey, contentSchemaVersion, versionId) is not null)
        {
            return true;
        }

        LogUnregisteredTaskType(taskId.Value);
        return false;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Task {TaskId} is left out of the task bank because its task type is no longer registered")]
    private partial void LogUnregisteredTaskType(Guid taskId);
}
