using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HireLab.SharedKernel.Events;

internal sealed partial class IntegrationEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventDispatcher> logger) : IIntegrationEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> _invokers = new();

    public async Task DispatchAsync(IReadOnlyList<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvents);

        foreach (var integrationEvent in integrationEvents)
        {
            var invoker = _invokers.GetOrAdd(
                integrationEvent.GetType(),
                static eventType => (HandlerInvoker)Activator.CreateInstance(
                    typeof(HandlerInvoker<>).MakeGenericType(eventType))!);

            await invoker.InvokeAsync(serviceProvider, integrationEvent, logger, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Integration event handler {HandlerName} failed for {EventName}")]
    private static partial void LogHandlerFailed(ILogger logger, Exception exception, string handlerName, string eventName);

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(
            IServiceProvider serviceProvider,
            IIntegrationEvent integrationEvent,
            ILogger logger,
            CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IIntegrationEvent
    {
        public override async Task InvokeAsync(
            IServiceProvider serviceProvider,
            IIntegrationEvent integrationEvent,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            {
                try
                {
                    await handler.HandleAsync((TEvent)integrationEvent, cancellationToken);
                }
                catch (Exception exception)
                {
                    LogHandlerFailed(logger, exception, handler.GetType().Name, typeof(TEvent).Name);
                }
            }
        }
    }
}
