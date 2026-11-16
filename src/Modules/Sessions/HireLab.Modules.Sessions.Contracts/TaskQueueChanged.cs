using HireLab.SharedKernel.Events;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Raised after a session's task queue changed: a task was added, removed or moved, or a task became Current or Done
/// (ADR 0031). <see cref="QueueRevision"/> is the session's queue revision after the change; readers refetch the queue
/// over HTTP (ADR 0006).
/// </summary>
public sealed record TaskQueueChanged(InterviewSessionId SessionId, int QueueRevision) : IIntegrationEvent;
