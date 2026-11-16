using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// One interview with a candidate (ADR 0031): <see cref="InterviewSessionState.Planned"/> →
/// <see cref="InterviewSessionState.Live"/> → <see cref="InterviewSessionState.Finished"/>, with an ordered queue of
/// <see cref="SessionTask"/>s. Shown as "Session N" for its candidate, where N is <see cref="Number"/>.
/// </summary>
internal sealed class InterviewSession : ITeamOwned
{
    public const int MaxTasks = 30;

    private readonly List<SessionTask> _tasks = [];

    private InterviewSession()
    {
    }

    public InterviewSessionId Id { get; private set; }

    public TeamId TeamId { get; private set; }

    public CandidateId CandidateId { get; private set; }

    /// <summary>The session's place among its candidate's sessions (1, 2, 3 …), shown as "Session N".</summary>
    public int Number { get; private set; }

    public DateTimeOffset? ScheduledAt { get; private set; }

    public InterviewSessionState State { get; private set; }

    public UserId CreatedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the session is first saved.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Raised by every change to the queue. It changes the session's row, so two concurrent queue changes can't both be
    /// saved, and it is the revision real-time events carry (ADR 0006).
    /// </summary>
    public int QueueRevision { get; private set; }

    public uint Version { get; private set; }

    /// <summary>The session tasks in no particular order; <see cref="Queue"/> orders them.</summary>
    public IReadOnlyCollection<SessionTask> Tasks => _tasks;

    public IReadOnlyList<SessionTask> Queue => [.. _tasks.OrderBy(task => task.Position)];

    public bool IsPlanned => State == InterviewSessionState.Planned;

    public static InterviewSession Plan(TeamId teamId, CandidateId candidateId, int number, DateTimeOffset? scheduledAt, UserId createdBy) =>
        new()
        {
            Id = InterviewSessionId.New(),
            TeamId = teamId,
            CandidateId = candidateId,
            Number = number,
            ScheduledAt = scheduledAt,
            State = InterviewSessionState.Planned,
            CreatedBy = createdBy,
        };

    public Result Reschedule(DateTimeOffset? scheduledAt)
    {
        if (!IsPlanned)
        {
            return InterviewSessionErrors.NotPlanned;
        }

        ScheduledAt = scheduledAt;
        return Result.Success();
    }

    public Result EnsureCanBeDeleted() => IsPlanned ? Result.Success() : InterviewSessionErrors.NotPlanned;

    /// <summary>Adds the task whose active version is <paramref name="snapshot"/> to the end of the queue.</summary>
    public Result<SessionTask> AddTask(TaskVersionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var editable = EnsureQueueEditable();
        if (editable.IsFailure)
        {
            return editable.Error;
        }

        if (_tasks.Any(task => task.TaskId == snapshot.TaskId))
        {
            return InterviewSessionErrors.TaskAlreadyQueued;
        }

        if (_tasks.Count >= MaxTasks)
        {
            return InterviewSessionErrors.QueueFull;
        }

        var added = SessionTask.Queue(Id, _tasks.Count, snapshot);
        _tasks.Add(added);
        QueueRevision++;
        return added;
    }

    public Result RemoveTask(SessionTaskId sessionTaskId)
    {
        var editable = EnsureQueueEditable();
        if (editable.IsFailure)
        {
            return editable;
        }

        var removed = _tasks.SingleOrDefault(task => task.Id == sessionTaskId);
        if (removed is null)
        {
            return InterviewSessionErrors.SessionTaskNotFound;
        }

        var remaining = Queue.Where(task => task != removed).ToList();
        _tasks.Remove(removed);
        Renumber(remaining);
        return Result.Success();
    }

    /// <summary>Puts the queue in <paramref name="order"/>, which must list every session task exactly once.</summary>
    public Result Reorder(IReadOnlyList<SessionTaskId> order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var editable = EnsureQueueEditable();
        if (editable.IsFailure)
        {
            return editable;
        }

        if (order.Count != _tasks.Count || order.Distinct().Count() != order.Count)
        {
            return InterviewSessionErrors.OrderMismatch;
        }

        var byId = _tasks.ToDictionary(task => task.Id);
        if (!order.All(byId.ContainsKey))
        {
            return InterviewSessionErrors.OrderMismatch;
        }

        Renumber([.. order.Select(id => byId[id])]);
        return Result.Success();
    }

    /// <summary>
    /// Re-pins a Pending session task to <paramref name="snapshot"/>, the current active version of its task, when that
    /// is another version (ADR 0028). Returns whether the pin changed.
    /// </summary>
    public bool Repin(SessionTaskId sessionTaskId, TaskVersionSnapshot snapshot)
    {
        var task = _tasks.SingleOrDefault(task => task.Id == sessionTaskId)
            ?? throw new ArgumentException("The task is not in this session's queue.", nameof(sessionTaskId));
        return task.Repin(snapshot);
    }

    private Result EnsureQueueEditable() => IsPlanned ? Result.Success() : InterviewSessionErrors.NotPlanned;

    private void Renumber(List<SessionTask> ordered)
    {
        for (var position = 0; position < ordered.Count; position++)
        {
            ordered[position].MoveTo(position);
        }

        QueueRevision++;
    }
}
