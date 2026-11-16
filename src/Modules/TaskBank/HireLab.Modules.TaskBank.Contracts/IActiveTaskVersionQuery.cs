namespace HireLab.Modules.TaskBank.Contracts;

/// <summary>
/// Reads the active version of a task for other modules: Sessions pins it when a task is queued and re-pins a
/// Pending session task lazily (ADR 0028). Registered as a scoped service by the TaskBank module. It sees what the
/// current staff user's task bank shows: the team's own tasks and Global tasks (system tasks included), never archived
/// ones, and nothing for the admin, a candidate's request or code outside a request (ADR 0015).
/// </summary>
public interface IActiveTaskVersionQuery
{
    /// <summary>
    /// The snapshot of the task's active version, or <c>null</c> when the task isn't in the current user's task bank or
    /// has no published version. Throws when the stored content doesn't match its registered task type (a missing
    /// type or another content schema version), which needs a fix rather than a fallback.
    /// </summary>
    Task<TaskVersionSnapshot?> FindAsync(TaskId taskId, CancellationToken cancellationToken = default);
}
