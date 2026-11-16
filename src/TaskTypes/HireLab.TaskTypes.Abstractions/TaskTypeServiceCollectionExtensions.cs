using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>Registers task types and the <see cref="ITaskTypeRegistry"/>.</summary>
public static class TaskTypeServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="ITaskTypeRegistry"/>, which holds every <see cref="ITaskType"/> registered in the container.</summary>
    public static IServiceCollection AddTaskTypeRegistry(this IServiceCollection services)
    {
        services.TryAddSingleton<ITaskTypeRegistry, TaskTypeRegistry>();
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TTaskType"/> as a singleton <see cref="ITaskType"/> and the registry. Registering
    /// the same class twice has no effect; two classes with the same key make the registry fail when it is created.
    /// </summary>
    public static IServiceCollection AddTaskType<TTaskType>(this IServiceCollection services)
        where TTaskType : class, ITaskType
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITaskType, TTaskType>());
        return services.AddTaskTypeRegistry();
    }
}
