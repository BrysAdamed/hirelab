using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Queue;
using HireLab.Modules.Sessions.Sessions;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Running.MarkSessionTaskDone;

internal sealed record MarkSessionTaskDoneCommand(InterviewSessionId SessionId, SessionTaskId SessionTaskId);

/// <summary>
/// Marks a Live session's Current task done. The next Pending task is re-pinned to its task's active version
/// (ADR 0028) and becomes Current; after the last task the session stays Live. Naming the task keeps a repeated
/// request from skipping one.
/// </summary>
internal sealed class MarkSessionTaskDoneHandler(SessionsDbContext dbContext, LazyRepinning repinning, IIntegrationEventQueue events)
    : ICommandHandler<MarkSessionTaskDoneCommand, Result>
{
    public async Task<Result> HandleAsync(MarkSessionTaskDoneCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        if (session.IsLive && session.NextTask is { } next)
        {
            await repinning.RepinAsync(session, next, cancellationToken);
        }

        var done = session.MarkDone(command.SessionTaskId);
        if (done.IsSuccess)
        {
            events.RaiseQueueChanged(session);
        }

        return done;
    }
}
