using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Queue.RemoveSessionTask;

internal sealed record RemoveSessionTaskCommand(InterviewSessionId SessionId, SessionTaskId SessionTaskId);

internal sealed class RemoveSessionTaskHandler(SessionsDbContext dbContext) : ICommandHandler<RemoveSessionTaskCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveSessionTaskCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        return session is null ? InterviewSessionErrors.NotFound : session.RemoveTask(command.SessionTaskId);
    }
}
