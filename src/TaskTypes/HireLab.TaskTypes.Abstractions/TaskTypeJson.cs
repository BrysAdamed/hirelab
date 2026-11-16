using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>
/// The JSON settings for task content, responses and rubrics. Use them wherever these are serialised, for example
/// into <c>jsonb</c>, so that stored documents read back the same way the task type validated them.
/// </summary>
public static class TaskTypeJson
{
    /// <summary>
    /// Web defaults (camelCase), strict numbers (ADR 0018), nullable annotations and required constructor parameters
    /// respected, and unknown properties rejected, so a typo in content fails validation instead of being dropped.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.Strict,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };

        options.MakeReadOnly();
        return options;
    }
}
