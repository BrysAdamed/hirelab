using HireLab.Modules.TaskBank.Domain;

namespace HireLab.Modules.TaskBank.Persistence;

/// <summary>A task in the task bank with its active version.</summary>
internal sealed class TaskWithActiveVersion
{
    public required InterviewTask Task { get; init; }

    public required TaskVersion Version { get; init; }
}

/// <summary>
/// Resolves active versions in the database, the query twin of <see cref="InterviewTask.ActiveVersion"/>: the version
/// a manager made active, otherwise the newest published one. Drafts are never active.
/// </summary>
internal static class ActiveTaskVersions
{
    /// <summary>
    /// The tasks in the current user's task bank, each with its active version: visible (the visibility filter applies),
    /// not archived and with at least one published version.
    /// </summary>
    public static IQueryable<TaskWithActiveVersion> TasksInBank(this TaskBankDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        return from task in dbContext.Tasks
               where !task.IsArchived
               join version in dbContext.TaskVersions on task.Id equals version.TaskId
               where version.State == TaskVersionState.Published
                   && (task.ActiveVersionId == null
                       ? !dbContext.TaskVersions.Any(newer =>
                           newer.TaskId == version.TaskId && newer.State == TaskVersionState.Published && newer.Number > version.Number)
                       : task.ActiveVersionId == version.Id)
               select new TaskWithActiveVersion { Task = task, Version = version };
    }
}
