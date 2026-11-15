using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Handlers.Decorators;

internal sealed class UnitOfWorkDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IUnitOfWork unitOfWork,
    IIntegrationEventQueue eventQueue,
    IIntegrationEventDispatcher eventDispatcher)
    : ICommandHandler<TCommand, TResult>
    where TCommand : notnull
    where TResult : Result, IResultFactory<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var result = await inner.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            eventQueue.DequeueAll();
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvents = eventQueue.DequeueAll();
        if (integrationEvents.Count > 0)
        {
            await eventDispatcher.DispatchAsync(integrationEvents, CancellationToken.None);
        }

        return result;
    }
}
