using HireLab.TaskTypes.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.TaskTypes.Catalog;

/// <summary>
/// The task types HireLab ships (ADR 0028). The API and the migration worker register this one list, so a new type is
/// added here once: a project reference and an <c>AddTaskType</c> call.
/// </summary>
public static class TaskTypeCatalog
{
    /// <summary>Registers the task type registry and every shipped task type.</summary>
    public static IServiceCollection AddTaskTypes(this IServiceCollection services) =>
        services.AddTaskTypeRegistry();
}
