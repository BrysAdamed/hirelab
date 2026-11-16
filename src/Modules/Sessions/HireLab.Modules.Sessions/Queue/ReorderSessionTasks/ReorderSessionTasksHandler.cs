using FluentValidation;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Sessions;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Queue.ReorderSessionTasks;

internal sealed record ReorderSessionTasksCommand(InterviewSessionId SessionId, IReadOnlyList<SessionTaskId> Order);

internal sealed class ReorderSessionTasksValidator : AbstractValidator<ReorderSessionTasksCommand>
{
    public ReorderSessionTasksValidator()
    {
        RuleFor(command => command.Order)
            .Must(order => order.Count <= InterviewSession.MaxTasks)
            .WithMessage($"A session's queue holds at most {InterviewSession.MaxTasks} tasks.");
    }
}

/// <summary>
/// Puts a session's queue in a new order, which must list every task in the queue exactly once and keep the tasks that
/// have started where they are. Refused once the session has finished.
/// </summary>
internal sealed class ReorderSessionTasksHandler(SessionsDbContext dbContext, IIntegrationEventQueue events)
    : ICommandHandler<ReorderSessionTasksCommand, Result>
{
    public async Task<Result> HandleAsync(ReorderSessionTasksCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.Include(session => session.Tasks)
            .SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        var reordered = session.Reorder(command.Order);
        if (reordered.IsSuccess)
        {
            events.RaiseQueueChanged(session);
        }

        return reordered;
    }
}
