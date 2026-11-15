namespace HireLab.SharedKernel.Events;

/// <summary>
/// Collects the integration events a command handler raises. The unit of work dispatches them after the
/// changes are saved, and discards them when the handler fails.
/// </summary>
public interface IIntegrationEventQueue
{
    void Enqueue(IIntegrationEvent integrationEvent);

    IReadOnlyList<IIntegrationEvent> DequeueAll();
}
