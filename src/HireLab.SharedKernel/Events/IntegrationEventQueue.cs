namespace HireLab.SharedKernel.Events;

internal sealed class IntegrationEventQueue : IIntegrationEventQueue
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        _events.Add(integrationEvent);
    }

    public IReadOnlyList<IIntegrationEvent> DequeueAll()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }
}
