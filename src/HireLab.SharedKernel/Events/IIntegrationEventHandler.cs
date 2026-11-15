namespace HireLab.SharedKernel.Events;

/// <summary>
/// Reacts to an integration event after the raising command has committed.
/// A failure is logged and doesn't affect the raising command or the other handlers.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
