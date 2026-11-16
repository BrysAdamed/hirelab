using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// One revision of a task (ADR 0015), part of the <see cref="InterviewTask"/> aggregate. It holds the validated details,
/// the candidate and staff content in two <c>jsonb</c> columns and the <see cref="ContentSchemaVersion"/> they were
/// validated with. Once published it never changes.
/// </summary>
internal sealed class TaskVersion
{
    private TaskVersion()
    {
    }

    public TaskVersionId Id { get; private set; }

    public TaskId TaskId { get; private set; }

    public int Number { get; private set; }

    public TaskVersionState State { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public TaskLevel Level { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public IReadOnlyList<string> Tags { get; private set; } = [];

    public string ReferenceAnswer { get; private set; } = string.Empty;

    public string? InterviewerNotes { get; private set; }

    public Rubric Rubric { get; private set; } = new([]);

    public JsonElement CandidateContent { get; private set; }

    public JsonElement StaffContent { get; private set; }

    public int ContentSchemaVersion { get; private set; }

    /// <summary>The SHA-256 of the validated details and content, which seed import compares to detect a changed version.</summary>
    public string ContentHash { get; private set; } = string.Empty;

    /// <summary>Who created the version; <c>null</c> for a system task's version.</summary>
    public UserId? CreatedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the version is first saved.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Who published the version; <c>null</c> while it is a draft and for a system task's version.</summary>
    public UserId? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public bool IsPublished => State == TaskVersionState.Published;

    public static TaskVersion CreateDraft(TaskId taskId, int number, ValidatedTaskVersion validated, UserId? createdBy)
    {
        ArgumentNullException.ThrowIfNull(validated);
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        var details = validated.Details;
        return new TaskVersion
        {
            Id = TaskVersionId.New(),
            TaskId = taskId,
            Number = number,
            State = TaskVersionState.Draft,
            Title = details.Title,
            Summary = details.Summary,
            Level = details.Level!.Value,
            EstimatedMinutes = details.EstimatedMinutes,
            Tags = [.. details.Tags!],
            ReferenceAnswer = details.ReferenceAnswer,
            InterviewerNotes = details.InterviewerNotes,
            Rubric = details.Rubric!,
            CandidateContent = details.CandidateContent.Clone(),
            StaffContent = details.StaffContent.Clone(),
            ContentSchemaVersion = validated.ContentSchemaVersion,
            ContentHash = validated.ContentHash,
            CreatedBy = createdBy,
        };
    }

    public void Publish(UserId? publishedBy, DateTimeOffset publishedAt)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException($"Task version {Id} is already published.");
        }

        State = TaskVersionState.Published;
        PublishedBy = publishedBy;
        PublishedAt = publishedAt;
    }
}
