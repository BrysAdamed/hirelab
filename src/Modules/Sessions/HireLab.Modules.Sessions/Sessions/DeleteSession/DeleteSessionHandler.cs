using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.DeleteSession;

internal sealed record DeleteSessionCommand(InterviewSessionId SessionId);

/// <summary>Deletes a planned session with its queue. A session that has gone Live is never deleted (ADR 0031).</summary>
internal sealed class DeleteSessionHandler(SessionsDbContext dbContext) : ICommandHandler<DeleteSessionCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        var deletable = session.EnsureCanBeDeleted();
        if (deletable.IsFailure)
        {
            return deletable;
        }

        dbContext.Sessions.Remove(session);
        return Result.Success();
    }
}
