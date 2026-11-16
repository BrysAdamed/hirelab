using HireLab.SharedKernel.Results;

namespace HireLab.Modules.TaskBank.Domain;

internal static class TaskErrors
{
    public static readonly Error NotFound = Error.NotFound("Task.NotFound", "The task was not found.");

    public static readonly Error VersionNotFound = Error.NotFound("Task.VersionNotFound", "The task version was not found.");

    public static readonly Error SystemTaskReadOnly = Error.Conflict(
        "Task.SystemTaskReadOnly",
        "This is a system task. System tasks are read-only; duplicate it to change it.");

    public static readonly Error Archived = Error.Conflict("Task.Archived", "This task is archived.");

    public static readonly Error DraftExists = Error.Conflict(
        "Task.DraftExists",
        "This task already has a draft. Edit or publish that draft instead.");

    public static readonly Error NoDraft = Error.Conflict("Task.NoDraft", "This task has no draft to publish.");

    public static readonly Error VersionNotPublished = Error.Conflict(
        "Task.VersionNotPublished",
        "Only a published version can be the active version.");
}
