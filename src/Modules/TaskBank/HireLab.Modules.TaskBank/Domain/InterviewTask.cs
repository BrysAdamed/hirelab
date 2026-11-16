using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// A task in the task bank (ADR 0015): one interview exercise of a task type, with its <see cref="Versions"/>. A team
/// task belongs to its team and is seen by that team, or by every team when <see cref="TaskVisibility.Global"/>. A
/// system task is imported from a seed package: Global, owned by no team, identified by its <see cref="Slug"/>, and
/// read-only. Named <c>InterviewTask</c> because <c>Task</c> clashes with .NET's. Load it with its versions.
/// </summary>
internal sealed class InterviewTask
{
    private readonly List<TaskVersion> _versions = [];

    private InterviewTask()
    {
    }

    public TaskId Id { get; private set; }

    /// <summary>The owning team; <c>null</c> for a system task.</summary>
    public TeamId? TeamId { get; private set; }

    public TaskVisibility Visibility { get; private set; }

    public bool IsSystem { get; private set; }

    /// <summary>The seed package's folder name, a system task's external key for idempotent import; <c>null</c> for a team task.</summary>
    public string? Slug { get; private set; }

    public string TaskTypeKey { get; private set; } = string.Empty;

    /// <summary>
    /// The version a manager made active. When it is <c>null</c>, the newest published version is active. A system task
    /// never has one.
    /// </summary>
    public TaskVersionId? ActiveVersionId { get; private set; }

    public bool IsArchived { get; private set; }

    public UserId? ArchivedBy { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Who created the task; <c>null</c> for a system task.</summary>
    public UserId? CreatedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the task is first saved.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public uint Version { get; private set; }

    public IReadOnlyCollection<TaskVersion> Versions => _versions;

    /// <summary>The version the task bank offers: the one a manager made active, otherwise the newest published one.</summary>
    public TaskVersion? ActiveVersion =>
        ActiveVersionId is { } activeVersionId
            ? _versions.Single(version => version.Id == activeVersionId)
            : _versions.Where(version => version.IsPublished).MaxBy(version => version.Number);

    /// <summary>The task's one unpublished version, if any.</summary>
    public TaskVersion? Draft => _versions.SingleOrDefault(version => !version.IsPublished);

    /// <summary>Creates a system task from its first seed package version, published at once.</summary>
    public static InterviewTask CreateSystem(string slug, ValidatedTaskVersion firstVersion, DateTimeOffset publishedAt)
    {
        ArgumentNullException.ThrowIfNull(firstVersion);
        if (!KebabCase.IsMatch(slug))
        {
            throw new ArgumentException($"A system task's slug must be kebab-case, but it is '{slug}'.", nameof(slug));
        }

        var task = new InterviewTask
        {
            Id = TaskId.New(),
            Visibility = TaskVisibility.Global,
            IsSystem = true,
            Slug = slug,
            TaskTypeKey = firstVersion.TaskTypeKey,
        };
        task.AddSystemVersion(firstVersion, publishedAt);
        return task;
    }

    /// <summary>Adds a system task's next seed package version, published at once, which makes it the active version.</summary>
    public TaskVersion AddSystemVersion(ValidatedTaskVersion validated, DateTimeOffset publishedAt)
    {
        if (!IsSystem)
        {
            throw new InvalidOperationException($"Task {Id} is a team task; only system tasks get seed versions.");
        }

        var version = AddVersion(validated, createdBy: null);
        version.Publish(publishedBy: null, publishedAt);
        return version;
    }

    /// <summary>Creates a team's task with its first version as a draft.</summary>
    public static InterviewTask CreateForTeam(TeamId teamId, TaskVisibility visibility, ValidatedTaskVersion firstVersion, UserId createdBy)
    {
        ArgumentNullException.ThrowIfNull(firstVersion);
        if (!Enum.IsDefined(visibility))
        {
            throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null);
        }

        var task = new InterviewTask
        {
            Id = TaskId.New(),
            TeamId = teamId,
            Visibility = visibility,
            TaskTypeKey = firstVersion.TaskTypeKey,
            CreatedBy = createdBy,
        };
        task.AddVersion(firstVersion, createdBy);
        return task;
    }

    /// <summary>Starts the task's next version as a draft. A task has at most one draft.</summary>
    public Result<TaskVersion> StartDraft(ValidatedTaskVersion validated, UserId createdBy)
    {
        if (EnsureEditable() is { IsFailure: true } notEditable)
        {
            return notEditable.Error;
        }

        return Draft is null ? AddVersion(validated, createdBy) : TaskErrors.DraftExists;
    }

    /// <summary>
    /// Publishes the draft. With <paramref name="makeActive"/> the newest published version, now the draft, becomes active;
    /// without it the version that was active stays active.
    /// </summary>
    public Result<TaskVersion> PublishDraft(bool makeActive, UserId publishedBy, DateTimeOffset publishedAt)
    {
        if (EnsureEditable() is { IsFailure: true } notEditable)
        {
            return notEditable.Error;
        }

        if (Draft is not { } draft)
        {
            return TaskErrors.NoDraft;
        }

        ActiveVersionId = makeActive ? null : ActiveVersion?.Id;
        draft.Publish(publishedBy, publishedAt);
        return draft;
    }

    /// <summary>Makes a published version the active one, which is also how a manager rolls back.</summary>
    public Result SwitchActiveVersion(TaskVersionId versionId)
    {
        if (EnsureEditable() is { IsFailure: true } notEditable)
        {
            return notEditable;
        }

        var version = _versions.SingleOrDefault(version => version.Id == versionId);
        if (version is null)
        {
            return TaskErrors.VersionNotFound;
        }

        if (!version.IsPublished)
        {
            return TaskErrors.VersionNotPublished;
        }

        ActiveVersionId = versionId;
        return Result.Success();
    }

    /// <summary>Takes the task out of the task bank for good. Sessions keep the snapshots they hold.</summary>
    public Result Archive(UserId archivedBy, DateTimeOffset archivedAt)
    {
        if (EnsureEditable() is { IsFailure: true } notEditable)
        {
            return notEditable;
        }

        IsArchived = true;
        ArchivedBy = archivedBy;
        ArchivedAt = archivedAt;
        return Result.Success();
    }

    private Result EnsureEditable()
    {
        if (IsSystem)
        {
            return TaskErrors.SystemTaskReadOnly;
        }

        return IsArchived ? TaskErrors.Archived : Result.Success();
    }

    private TaskVersion AddVersion(ValidatedTaskVersion validated, UserId? createdBy)
    {
        ArgumentNullException.ThrowIfNull(validated);
        if (validated.TaskTypeKey != TaskTypeKey)
        {
            throw new ArgumentException(
                $"The version was validated for task type '{validated.TaskTypeKey}', but task {Id} is of type '{TaskTypeKey}'.",
                nameof(validated));
        }

        var number = _versions.Count == 0 ? 1 : _versions.Max(version => version.Number) + 1;
        var version = TaskVersion.CreateDraft(Id, number, validated, createdBy);
        _versions.Add(version);
        return version;
    }
}
