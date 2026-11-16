using System.Reflection;
using FluentValidation;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers.Decorators;
using HireLab.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HireLab.SharedKernel.Handlers;

/// <summary>
/// Registers a module's handlers, validators and integration event handlers in one call.
/// </summary>
public static class HandlerRegistration
{
    private static readonly MethodInfo _addCommandHandler = typeof(HandlerRegistration)
        .GetMethod(nameof(AddCommandHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo _addQueryHandler = typeof(HandlerRegistration)
        .GetMethod(nameof(AddQueryHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Scans <paramref name="assembly"/> (internal types included) and registers, as scoped services:
    /// <list type="bullet">
    /// <item>
    /// command handlers, decorated as logging → validation → unit of work on <typeparamref name="TUnitOfWork"/>, which
    /// saves a successful command and turns a lost concurrency race into <see cref="Error.ChangedMeanwhile"/>;
    /// </item>
    /// <item>query handlers, decorated as logging → validation;</item>
    /// <item>FluentValidation validators;</item>
    /// <item>integration event handlers.</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TUnitOfWork">The module's <c>DbContext</c>. It must be registered in the container by the module.</typeparam>
    public static IServiceCollection AddHandlers<TUnitOfWork>(this IServiceCollection services, Assembly assembly)
        where TUnitOfWork : class, IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.TryAddScoped<IIntegrationEventQueue, IntegrationEventQueue>();
        services.TryAddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        foreach (var type in assembly.DefinedTypes.Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }))
        {
            foreach (var serviceType in type.ImplementedInterfaces.Where(serviceType => serviceType.IsGenericType))
            {
                var definition = serviceType.GetGenericTypeDefinition();
                var arguments = serviceType.GenericTypeArguments;

                if (definition == typeof(ICommandHandler<,>))
                {
                    _addCommandHandler
                        .MakeGenericMethod(type, arguments[0], arguments[1], typeof(TUnitOfWork))
                        .Invoke(null, [services]);
                }
                else if (definition == typeof(IQueryHandler<,>))
                {
                    _addQueryHandler
                        .MakeGenericMethod(type, arguments[0], arguments[1])
                        .Invoke(null, [services]);
                }
                else if (definition == typeof(IIntegrationEventHandler<>))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, type));
                }
            }
        }

        return services;
    }

    private static void AddCommandHandler<THandler, TCommand, TResult, TUnitOfWork>(IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TCommand : notnull
        where TResult : Result, IResultFactory<TResult>
        where TUnitOfWork : class, IUnitOfWork
    {
        services.TryAddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResult>>(provider =>
            new LoggingDecorator.Command<TCommand, TResult>(
                new ValidationDecorator.Command<TCommand, TResult>(
                    new UnitOfWorkDecorator<TCommand, TResult>(
                        provider.GetRequiredService<THandler>(),
                        provider.GetRequiredService<TUnitOfWork>(),
                        provider.GetRequiredService<IIntegrationEventQueue>(),
                        provider.GetRequiredService<IIntegrationEventDispatcher>()),
                    provider.GetServices<IValidator<TCommand>>()),
                provider.GetRequiredService<ILogger<THandler>>()));
    }

    private static void AddQueryHandler<THandler, TQuery, TResult>(IServiceCollection services)
        where THandler : class, IQueryHandler<TQuery, TResult>
        where TQuery : notnull
        where TResult : Result, IResultFactory<TResult>
    {
        services.TryAddScoped<THandler>();
        services.AddScoped<IQueryHandler<TQuery, TResult>>(provider =>
            new LoggingDecorator.Query<TQuery, TResult>(
                new ValidationDecorator.Query<TQuery, TResult>(
                    provider.GetRequiredService<THandler>(),
                    provider.GetServices<IValidator<TQuery>>()),
                provider.GetRequiredService<ILogger<THandler>>()));
    }
}
