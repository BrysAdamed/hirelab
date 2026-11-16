using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.TaskTypes.Abstractions;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.Sessions.Queue;

/// <summary>
/// Lets the Pending tasks of a session follow their task's active version (ADR 0028, ADR 0031): each one whose task now
/// has another active version is re-pinned with a fresh snapshot. A task that left the task bank (archived, or no
/// longer visible to the team) or whose task type is no longer registered keeps its pin, which is logged by id only.
/// </summary>
internal sealed partial class LazyRepinning(
    IActiveTaskVersionQuery activeVersions,
    ITaskTypeRegistry taskTypes,
    ILogger<LazyRepinning> logger)
{
    /// <summary>Re-pins <paramref name="session"/>'s Pending tasks and returns whether any pin changed.</summary>
    public async Task<bool> RepinPendingTasksAsync(InterviewSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var changed = false;
        foreach (var task in session.Queue.Where(task => task.IsPending))
        {
            if (taskTypes.Find(task.TaskTypeKey) is null)
            {
                LogUnregisteredTaskType(session.Id.Value, task.Id.Value, task.TaskId.Value);
                continue;
            }

            var snapshot = await activeVersions.FindAsync(task.TaskId, cancellationToken);
            if (snapshot is null)
            {
                LogTaskNotInBank(session.Id.Value, task.Id.Value, task.TaskId.Value);
                continue;
            }

            var previousVersionId = task.TaskVersionId;
            if (session.Repin(task.Id, snapshot))
            {
                LogRepinned(session.Id.Value, task.Id.Value, previousVersionId.Value, snapshot.VersionId.Value);
                changed = true;
            }
        }

        return changed;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Session task {SessionTaskId} of session {SessionId} re-pinned from task version {PreviousVersionId} to {VersionId}")]
    private partial void LogRepinned(Guid sessionId, Guid sessionTaskId, Guid previousVersionId, Guid versionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Session task {SessionTaskId} of session {SessionId} keeps its pin because task {TaskId} is no longer in the task bank")]
    private partial void LogTaskNotInBank(Guid sessionId, Guid sessionTaskId, Guid taskId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Session task {SessionTaskId} of session {SessionId} keeps its pin because the task type of task {TaskId} is no longer registered")]
    private partial void LogUnregisteredTaskType(Guid sessionId, Guid sessionTaskId, Guid taskId);
}
