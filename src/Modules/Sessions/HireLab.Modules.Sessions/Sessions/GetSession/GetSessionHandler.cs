using HireLab.Modules.Identity.Contracts;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Queue;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.GetSession;

internal sealed record GetSessionQuery(InterviewSessionId SessionId);

/// <summary>
/// A session with its queue in order. <see cref="CandidateDisplayName"/> is read from Positions each time (ADR 0023);
/// <see cref="CreatedByName"/> is <c>null</c> when the user is unknown. <see cref="QueueRevision"/> changes with every
/// queue change.
/// </summary>
internal sealed record SessionResponse(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    Guid CandidateId,
    string CandidateDisplayName,
    Guid CreatedById,
    string? CreatedByName,
    DateTimeOffset CreatedAt,
    int QueueRevision,
    IReadOnlyList<SessionTaskResponse> Tasks);

/// <summary>A task in the queue with the task version it pins (<c>vN</c>).</summary>
internal sealed record SessionTaskResponse(
    Guid Id,
    Guid TaskId,
    SessionTaskState State,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary);

/// <summary>
/// Opens a session's queue. Opening it re-pins the Pending tasks whose task has another active version since they were
/// queued (ADR 0028) and saves that; a concurrent change to the queue wins, and the next open re-pins.
/// </summary>
internal sealed class GetSessionHandler(
    SessionsDbContext dbContext,
    LazyRepinning repinning,
    ICandidateDirectory candidates,
    IStaffDirectory staff)
    : IQueryHandler<GetSessionQuery, Result<SessionResponse>>
{
    public async Task<Result<SessionResponse>> HandleAsync(GetSessionQuery query, CancellationToken cancellationToken)
    {
        var session = await LoadAsync(query.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        if (await repinning.RepinPendingTasksAsync(session, cancellationToken))
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                session = await LoadAsync(query.SessionId, cancellationToken);
                if (session is null)
                {
                    return InterviewSessionErrors.NotFound;
                }
            }
        }

        var candidate = await candidates.FindAsync(session.CandidateId, cancellationToken);
        var createdBy = await staff.FindDisplayNamesAsync([session.CreatedBy], cancellationToken);

        return new SessionResponse(
            session.Id.Value,
            session.Number,
            session.State,
            session.ScheduledAt,
            session.CandidateId.Value,
            candidate?.DisplayName ?? CandidateNames.Unknown,
            session.CreatedBy.Value,
            createdBy.GetValueOrDefault(session.CreatedBy),
            session.CreatedAt,
            session.QueueRevision,
            [.. session.Queue.Select(task => new SessionTaskResponse(
                task.Id.Value,
                task.TaskId.Value,
                task.State,
                task.VersionNumber,
                task.TaskTypeKey,
                task.Title,
                task.Summary))]);
    }

    private Task<InterviewSession?> LoadAsync(InterviewSessionId sessionId, CancellationToken cancellationToken) =>
        dbContext.Sessions.Include(session => session.Tasks).SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
}
