namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// Task version details that passed <see cref="TaskVersionValidator"/> for <see cref="TaskTypeKey"/>: normalised, with
/// the type's <see cref="ContentSchemaVersion"/> and the <see cref="ContentHash"/>. The task aggregate creates versions
/// only from it. Only the validator creates one.
/// </summary>
internal sealed class ValidatedTaskVersion
{
    internal ValidatedTaskVersion(string taskTypeKey, int contentSchemaVersion, TaskVersionDetails details, string contentHash)
    {
        TaskTypeKey = taskTypeKey;
        ContentSchemaVersion = contentSchemaVersion;
        Details = details;
        ContentHash = contentHash;
    }

    public string TaskTypeKey { get; }

    public int ContentSchemaVersion { get; }

    public TaskVersionDetails Details { get; }

    public string ContentHash { get; }
}
