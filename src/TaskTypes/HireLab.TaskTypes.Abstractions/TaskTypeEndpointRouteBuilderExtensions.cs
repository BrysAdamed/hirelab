using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>Maps the candidate endpoints of the registered task types.</summary>
public static class TaskTypeEndpointRouteBuilderExtensions
{
    /// <summary>The route prefix of task type endpoints within the candidate group.</summary>
    public const string CandidateRoutePrefix = "/tasks";

    /// <summary>
    /// Maps every <see cref="ITaskTypeEndpoints"/> task type into <c>/tasks/&lt;key&gt;</c> under <paramref name="candidate"/>.
    /// Resolving the registry here also makes an invalid registration, such as a duplicate key, fail at startup.
    /// </summary>
    public static IEndpointRouteBuilder MapTaskTypeCandidateEndpoints(this IEndpointRouteBuilder candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var registry = candidate.ServiceProvider.GetRequiredService<ITaskTypeRegistry>();
        foreach (var taskType in registry.All)
        {
            if (taskType is ITaskTypeEndpoints endpoints)
            {
                endpoints.MapCandidateEndpoints(candidate.MapGroup($"{CandidateRoutePrefix}/{taskType.Key}"));
            }
        }

        return candidate;
    }
}
