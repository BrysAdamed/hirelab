using System.Collections.Concurrent;
using HireLab.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Testing;

/// <summary>Records every integration event the API dispatches, in order. Emptied before every test.</summary>
public sealed class RaisedIntegrationEvents
{
    private readonly ConcurrentQueue<IIntegrationEvent> _events = new();

    /// <summary>The dispatched events of type <typeparamref name="TEvent"/>, oldest first.</summary>
    public IReadOnlyList<TEvent> OfType<TEvent>()
        where TEvent : IIntegrationEvent => [.. _events.OfType<TEvent>()];

    /// <summary>Forgets every recorded event.</summary>
    public void Reset() => _events.Clear();

    internal static void Record(IServiceCollection services, RaisedIntegrationEvents raised)
    {
        services.AddSingleton(raised);
        services.AddScoped(typeof(IIntegrationEventHandler<>), typeof(Recorder<>));
    }

    private sealed class Recorder<TEvent>(RaisedIntegrationEvents raised) : IIntegrationEventHandler<TEvent>
        where TEvent : IIntegrationEvent
    {
        public Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
        {
            raised._events.Enqueue(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
