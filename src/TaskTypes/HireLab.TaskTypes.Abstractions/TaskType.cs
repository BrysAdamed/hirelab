using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>
/// The base class of every task type. It reads the stored JSON into the typed models with <see cref="TaskTypeJson.Options"/>
/// and reports JSON that doesn't match them as a <see cref="ValidationError"/> keyed by
/// <see cref="CandidateContentField"/>, <see cref="StaffContentField"/> or <see cref="ResponseField"/>, before the
/// type's own rules run.
/// </summary>
public abstract class TaskType<TCandidateContent, TStaffContent, TResponse> : ITaskType<TCandidateContent, TStaffContent, TResponse>
    where TCandidateContent : class
    where TStaffContent : class
    where TResponse : class
{
    /// <summary>The validation error key for a candidate content part that doesn't match its model.</summary>
    public const string CandidateContentField = "candidateContent";

    /// <summary>The validation error key for a staff content part that doesn't match its model.</summary>
    public const string StaffContentField = "staffContent";

    /// <summary>The validation error key for a response that doesn't match its model.</summary>
    public const string ResponseField = "response";

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public Type CandidateContentType => typeof(TCandidateContent);

    /// <inheritdoc />
    public Type StaffContentType => typeof(TStaffContent);

    /// <inheritdoc />
    public Type ResponseType => typeof(TResponse);

    /// <inheritdoc />
    public abstract int ContentSchemaVersion { get; }

    /// <inheritdoc />
    public abstract int ResponseSchemaVersion { get; }

    /// <inheritdoc />
    public abstract Result ValidateContent(TaskContent<TCandidateContent, TStaffContent> content);

    /// <inheritdoc />
    public abstract Result ValidateResponse(TResponse response);

    /// <inheritdoc />
    /// <remarks>Suggests nothing unless the type overrides it.</remarks>
    public virtual IReadOnlyList<CriterionSuggestion> SuggestScores(
        TaskContent<TCandidateContent, TStaffContent> content,
        Rubric rubric,
        TResponse response) => [];

    Result ITaskType.ValidateContent(JsonElement candidateContent, JsonElement staffContent)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var candidate = Read<TCandidateContent>(candidateContent, CandidateContentField, errors);
        var staff = Read<TStaffContent>(staffContent, StaffContentField, errors);

        return candidate is not null && staff is not null
            ? ValidateContent(new TaskContent<TCandidateContent, TStaffContent>(candidate, staff))
            : new ValidationError(errors);
    }

    Result ITaskType.ValidateResponse(JsonElement response)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var typedResponse = Read<TResponse>(response, ResponseField, errors);

        return typedResponse is not null ? ValidateResponse(typedResponse) : new ValidationError(errors);
    }

    IReadOnlyList<CriterionSuggestion> ITaskType.SuggestScores(
        JsonElement candidateContent,
        JsonElement staffContent,
        Rubric rubric,
        JsonElement response) =>
        SuggestScores(
            new TaskContent<TCandidateContent, TStaffContent>(ReadValid<TCandidateContent>(candidateContent), ReadValid<TStaffContent>(staffContent)),
            rubric,
            ReadValid<TResponse>(response));

    private static T? Read<T>(JsonElement json, string field, Dictionary<string, string[]> errors)
        where T : class
    {
        try
        {
            var value = json.Deserialize<T>(TaskTypeJson.Options);
            if (value is null)
            {
                errors[field] = ["It is required."];
            }

            return value;
        }
        catch (JsonException exception)
        {
            errors[field] = [$"It doesn't match the task type's model at {exception.Path ?? "$"}."];
            return null;
        }
    }

    private T ReadValid<T>(JsonElement json)
        where T : class =>
        json.Deserialize<T>(TaskTypeJson.Options)
        ?? throw new InvalidOperationException($"Stored JSON for task type '{Key}' is null where a {typeof(T).Name} was expected.");
}
