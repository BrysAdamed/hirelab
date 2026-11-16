using Microsoft.AspNetCore.Routing;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>
/// Implemented by a task type that needs its own candidate endpoints, such as the AI chat proxy. They inherit the
/// candidate group's authorisation, antiforgery and OpenAPI document, take the session from the cookie, and must
/// return only candidate-safe data.
/// </summary>
public interface ITaskTypeEndpoints
{
    /// <summary>Maps the type's endpoints into its own group, <c>/api/candidate/tasks/&lt;key&gt;</c>.</summary>
    void MapCandidateEndpoints(IEndpointRouteBuilder taskType);
}
