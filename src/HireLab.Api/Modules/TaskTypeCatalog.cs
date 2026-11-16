using HireLab.TaskTypes.Abstractions;

namespace HireLab.Api.Modules;

internal static class TaskTypeCatalog
{
    public static IServiceCollection AddTaskTypes(this IServiceCollection services) =>
        services.AddTaskTypeRegistry();
}
