using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.RescheduleSession;

internal sealed record RescheduleSessionCommand(InterviewSessionId SessionId, DateTimeOffset? ScheduledAt);

internal sealed class RescheduleSessionHandler(SessionsDbContext dbContext) : ICommandHandler<RescheduleSessionCommand, Result>
{
    public async Task<Result> HandleAsync(RescheduleSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        return session is null ? InterviewSessionErrors.NotFound : session.Reschedule(command.ScheduledAt);
    }
}
