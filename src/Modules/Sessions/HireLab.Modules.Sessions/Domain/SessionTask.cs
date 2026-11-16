using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// One task in an <see cref="InterviewSession"/>'s queue, part of the session aggregate (ADR 0031). It pins a task
/// version and keeps a snapshot of it, so the session never depends on the task bank once the task has started. While
/// <see cref="SessionTaskState.Pending"/> it follows the task's active version lazily (ADR 0028).
/// </summary>
internal sealed class SessionTask
{
    private SessionTask()
    {
    }

    public SessionTaskId Id { get; private set; }

    public InterviewSessionId SessionId { get; private set; }

    /// <summary>The task's place in the queue, from 0.</summary>
    public int Position { get; private set; }

    public SessionTaskState State { get; private set; }

    public TaskId TaskId { get; private set; }

    public TaskVersionId TaskVersionId { get; private set; }

    public int VersionNumber { get; private set; }

    public string TaskTypeKey { get; private set; } = string.Empty;

    public int ContentSchemaVersion { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public JsonElement CandidateContent { get; private set; }

    public JsonElement StaffContent { get; private set; }

    public string ReferenceAnswer { get; private set; } = string.Empty;

    public string? InterviewerNotes { get; private set; }

    public Rubric Rubric { get; private set; } = new([]);

    public bool IsPending => State == SessionTaskState.Pending;

    public bool IsCurrent => State == SessionTaskState.Current;

    internal static SessionTask Queue(InterviewSessionId sessionId, int position, TaskVersionSnapshot snapshot)
    {
        var task = new SessionTask
        {
            Id = SessionTaskId.New(),
            SessionId = sessionId,
            Position = position,
            State = SessionTaskState.Pending,
            TaskId = snapshot.TaskId,
        };
        task.Pin(snapshot);
        return task;
    }

    internal void MoveTo(int position) => Position = position;

    internal void Start()
    {
        if (!IsPending)
        {
            throw new InvalidOperationException("Only a Pending session task can start.");
        }

        State = SessionTaskState.Current;
    }

    internal void Complete()
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Only the Current session task can be done.");
        }

        State = SessionTaskState.Done;
    }

    /// <summary>
    /// Pins <paramref name="snapshot"/> when it is another version of this task and the task hasn't started. Returns
    /// whether the pin changed.
    /// </summary>
    internal bool Repin(TaskVersionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.TaskId != TaskId)
        {
            throw new ArgumentException("The snapshot is of another task.", nameof(snapshot));
        }

        if (!IsPending || snapshot.VersionId == TaskVersionId)
        {
            return false;
        }

        Pin(snapshot);
        return true;
    }

    private void Pin(TaskVersionSnapshot snapshot)
    {
        TaskVersionId = snapshot.VersionId;
        VersionNumber = snapshot.Number;
        TaskTypeKey = snapshot.TaskTypeKey;
        ContentSchemaVersion = snapshot.ContentSchemaVersion;
        Title = snapshot.Title;
        Summary = snapshot.Summary;
        CandidateContent = snapshot.CandidateContent.Clone();
        StaffContent = snapshot.StaffContent.Clone();
        ReferenceAnswer = snapshot.ReferenceAnswer;
        InterviewerNotes = snapshot.InterviewerNotes;
        Rubric = snapshot.Rubric;
    }
}
