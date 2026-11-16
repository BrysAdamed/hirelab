using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Queue.AddSessionTask;

internal sealed record AddSessionTaskCommand(InterviewSessionId SessionId, TaskId TaskId);

internal sealed record AddedSessionTaskResponse(Guid Id);

/// <summary>
/// Queues a task from the caller's task bank at the end of a planned session's queue, pinning its active version with
/// a snapshot read through <see cref="IActiveTaskVersionQuery"/> (ADR 0029).
/// </summary>
internal sealed class AddSessionTaskHandler(SessionsDbContext dbContext, IActiveTaskVersionQuery activeVersions)
    : ICommandHandler<AddSessionTaskCommand, Result<AddedSessionTaskResponse>>
{
    public async Task<Result<AddedSessionTaskResponse>> HandleAsync(AddSessionTaskCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        var snapshot = await activeVersions.FindAsync(command.TaskId, cancellationToken);
        if (snapshot is null)
        {
            return InterviewSessionErrors.TaskNotFound;
        }

        var added = session.AddTask(snapshot);
        return added.IsSuccess ? new AddedSessionTaskResponse(added.Value.Id.Value) : added.Error;
    }
}
