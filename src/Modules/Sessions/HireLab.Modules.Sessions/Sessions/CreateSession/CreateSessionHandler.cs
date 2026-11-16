using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Persistence.Configurations;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.CreateSession;

internal sealed record CreateSessionCommand(CandidateId CandidateId, DateTimeOffset? ScheduledAt);

internal sealed record CreatedSessionResponse(Guid Id, int Number);

/// <summary>
/// Plans a session for one of the team's candidates with the candidate's next session number. Refused for an anonymised
/// candidate and a closed position, read through <see cref="ICandidateDirectory"/> (ADR 0023). Of two concurrent
/// creations for the same candidate, the one that loses the number fails with <c>Concurrency.ChangedMeanwhile</c>.
/// </summary>
internal sealed class CreateSessionHandler(SessionsDbContext dbContext, ICandidateDirectory candidates, ICurrentUser currentUser)
    : ICommandHandler<CreateSessionCommand, Result<CreatedSessionResponse>>
{
    public async Task<Result<CreatedSessionResponse>> HandleAsync(CreateSessionCommand command, CancellationToken cancellationToken)
    {
        var candidate = await candidates.FindAsync(command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return InterviewSessionErrors.CandidateNotFound;
        }

        if (candidate.IsJobPositionClosed)
        {
            return InterviewSessionErrors.PositionClosed;
        }

        if (candidate.IsAnonymised)
        {
            return InterviewSessionErrors.CandidateAnonymised;
        }

        var lastNumber = await dbContext.Sessions
            .Where(session => session.CandidateId == command.CandidateId)
            .MaxAsync(session => (int?)session.Number, cancellationToken) ?? 0;
        var session = InterviewSession.Plan(
            currentUser.TeamId ?? throw new InvalidOperationException("Only a staff user with a team can plan a session."),
            command.CandidateId,
            lastNumber + 1,
            command.ScheduledAt,
            currentUser.Id);

        dbContext.Sessions.Add(session);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.ViolatedConstraint() == InterviewSessionConfiguration.NumberIndex)
        {
            dbContext.ChangeTracker.Clear();
            return Error.ChangedMeanwhile;
        }

        return new CreatedSessionResponse(session.Id.Value, session.Number);
    }
}
