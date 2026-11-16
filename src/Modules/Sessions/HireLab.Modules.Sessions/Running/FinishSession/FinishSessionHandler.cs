using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Sessions;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Running.FinishSession;

internal sealed record FinishSessionCommand(InterviewSessionId SessionId);

/// <summary>
/// Finishes a Live session for good (ADR 0031): the Current task becomes Done and Pending tasks stay Pending, shown as
/// "not reached".
/// </summary>
internal sealed class FinishSessionHandler(SessionsDbContext dbContext, IIntegrationEventQueue events)
    : ICommandHandler<FinishSessionCommand, Result>
{
    public async Task<Result> HandleAsync(FinishSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        var revision = session.QueueRevision;
        var finished = session.Finish();
        if (finished.IsFailure)
        {
            return finished;
        }

        events.RaiseStateChanged(session);
        if (session.QueueRevision != revision)
        {
            events.RaiseQueueChanged(session);
        }

        return finished;
    }
}
