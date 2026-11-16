using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Queue;
using HireLab.Modules.Sessions.Sessions;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Running.GoLive;

internal sealed record GoLiveCommand(InterviewSessionId SessionId);

/// <summary>
/// Starts a planned session (ADR 0031): refused with an empty queue, for an anonymised candidate and for a closed
/// position. The first task is re-pinned to its task's active version (ADR 0028) and becomes Current.
/// </summary>
internal sealed class GoLiveHandler(
    SessionsDbContext dbContext,
    ICandidateDirectory candidates,
    LazyRepinning repinning,
    IIntegrationEventQueue events)
    : ICommandHandler<GoLiveCommand, Result>
{
    public async Task<Result> HandleAsync(GoLiveCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        if (!session.IsPlanned)
        {
            return InterviewSessionErrors.NotPlanned;
        }

        var eligibility = CandidateEligibility.Check(await candidates.FindAsync(session.CandidateId, cancellationToken));
        if (eligibility.IsFailure)
        {
            return eligibility;
        }

        if (session.NextTask is { } first)
        {
            await repinning.RepinAsync(session, first, cancellationToken);
        }

        var live = session.GoLive();
        if (live.IsFailure)
        {
            return live;
        }

        events.RaiseStateChanged(session);
        events.RaiseQueueChanged(session);
        return live;
    }
}
