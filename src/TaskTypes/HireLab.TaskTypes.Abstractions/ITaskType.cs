using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>
/// A task type: the kind of task and the code that comes with it (ADR 0013, ADR 0028). Modules work with this
/// untyped view, where content and responses are the JSON stored in <c>jsonb</c>. Implement a task type by deriving
/// from <see cref="TaskType{TCandidateContent, TStaffContent, TResponse}"/> and register it with
/// <see cref="TaskTypeServiceCollectionExtensions.AddTaskType{TTaskType}"/>.
/// </summary>
public interface ITaskType
{
    /// <summary>The stable kebab-case key that tasks, seed packages, routes and the frontend registries use, for example <c>code-review</c>.</summary>
    string Key { get; }

    /// <summary>The model of the content part the candidate sees.</summary>
    Type CandidateContentType { get; }

    /// <summary>The model of the staff-only content part. It never reaches the candidate API.</summary>
    Type StaffContentType { get; }

    /// <summary>The model of a candidate's response.</summary>
    Type ResponseType { get; }

    /// <summary>The version of the content models. Every task version stores the one it was written with.</summary>
    int ContentSchemaVersion { get; }

    /// <summary>The version of the response model. Every response stores the one it was written with.</summary>
    int ResponseSchemaVersion { get; }

    /// <summary>
    /// Validates a task version's content: both parts must match the content models, then pass the type's own rules.
    /// Runs on save, on publish and on seed import. A failure is a <see cref="ValidationError"/>.
    /// </summary>
    Result ValidateContent(JsonElement candidateContent, JsonElement staffContent);

    /// <summary>Validates a candidate's response against the response model and the type's own rules. A failure is a <see cref="ValidationError"/>.</summary>
    Result ValidateResponse(JsonElement response);

    /// <summary>
    /// Suggests values for rubric criteria from a response, which the interviewer confirms or overrides.
    /// The content and response must already be valid.
    /// </summary>
    IReadOnlyList<CriterionSuggestion> SuggestScores(JsonElement candidateContent, JsonElement staffContent, Rubric rubric, JsonElement response);
}
