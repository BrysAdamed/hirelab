using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Sessions;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Queue.RemoveSessionTask;

internal sealed record RemoveSessionTaskCommand(InterviewSessionId SessionId, SessionTaskId SessionTaskId);

/// <summary>Removes a task that hasn't started from a session's queue. Refused once the session has finished.</summary>
internal sealed class RemoveSessionTaskHandler(SessionsDbContext dbContext, IIntegrationEventQueue events)
    : ICommandHandler<RemoveSessionTaskCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveSessionTaskCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        var removed = session.RemoveTask(command.SessionTaskId);
        if (removed.IsSuccess)
        {
            events.RaiseQueueChanged(session);
        }

        return removed;
    }
}
