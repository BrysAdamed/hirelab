namespace HireLab.SharedKernel.Events;

/// <summary>
/// Runs every registered <see cref="IIntegrationEventHandler{TEvent}"/> for each event, in process.
/// Called by the unit of work after the commit; nothing is persisted, so events are lost if the process stops
/// before dispatch (ADR 0001).
/// </summary>
public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken);
}
