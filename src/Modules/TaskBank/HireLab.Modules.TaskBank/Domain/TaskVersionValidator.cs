using System.Security.Cryptography;
using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// The one validation of task version details, run on seed import and on save and publish in the editor (ADR 0015):
/// the generic rules (metadata, tags, reference answer, rubric, content size), then the task type's
/// <see cref="ITaskType.ValidateContent"/>. A failure is a <see cref="ValidationError"/> with every broken rule keyed by
/// its field, for example <c>title</c>, <c>tags[2]</c>, <c>rubric.criteria[0].weight</c> or the type's
/// <c>candidateContent.*</c> keys.
/// </summary>
internal sealed class TaskVersionValidator(ITaskTypeRegistry taskTypes)
{
    public const int MinScaleWeight = 1;
    public const int MaxScaleWeight = 10;

    public Result<ValidatedTaskVersion> Validate(string taskTypeKey, TaskVersionDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var taskType = taskTypes.Find(taskTypeKey ?? string.Empty);
        if (taskType is null)
        {
            Add(errors, "taskType", "Choose a registered task type.");
        }

        ValidateMetadata(details, errors);
        var tags = NormaliseTags(details.Tags, errors);
        ValidateRubric(details.Rubric, errors);
        var contentFits = ValidateContentSize(details, errors);

        if (taskType is not null && contentFits)
        {
            AddTaskTypeErrors(taskType.ValidateContent(details.CandidateContent, details.StaffContent), errors);
        }

        if (errors.Count > 0)
        {
            return new ValidationError(errors.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.Ordinal));
        }

        var normalised = details with
        {
            Title = details.Title.Trim(),
            Summary = details.Summary.Trim(),
            Tags = tags,
            InterviewerNotes = string.IsNullOrWhiteSpace(details.InterviewerNotes) ? null : details.InterviewerNotes,
        };

        return new ValidatedTaskVersion(taskType!.Key, taskType.ContentSchemaVersion, normalised, HashOf(taskType, normalised));
    }

    private static void ValidateMetadata(TaskVersionDetails details, Dictionary<string, List<string>> errors)
    {
        ValidateText(details.Title, "title", "Enter a title.", TaskVersionDetails.TitleMaxLength, errors);
        ValidateText(details.Summary, "summary", "Enter a summary.", TaskVersionDetails.SummaryMaxLength, errors);

        if (details.Level is not { } level || !Enum.IsDefined(level))
        {
            Add(errors, "level", "Choose a level.");
        }

        if (details.EstimatedMinutes is < TaskVersionDetails.MinEstimatedMinutes or > TaskVersionDetails.MaxEstimatedMinutes)
        {
            Add(errors, "estimatedMinutes", $"Estimate between {TaskVersionDetails.MinEstimatedMinutes} and {TaskVersionDetails.MaxEstimatedMinutes} minutes.");
        }

        if (string.IsNullOrWhiteSpace(details.ReferenceAnswer))
        {
            Add(errors, "referenceAnswer", "Enter a reference answer.");
        }
    }

    private static void ValidateText(string? value, string field, string requiredMessage, int maxLength, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, requiredMessage);
        }
        else if (value.Trim().Length > maxLength)
        {
            Add(errors, field, $"Use at most {maxLength} characters.");
        }
    }

    private static List<string> NormaliseTags(IReadOnlyList<string>? tags, Dictionary<string, List<string>> errors)
    {
        var normalised = new List<string>();
        for (var index = 0; index < (tags?.Count ?? 0); index++)
        {
            var tag = TaskTags.Normalise(tags![index]);
            if (!TaskTags.IsValid(tag))
            {
                Add(errors, $"tags[{index}]", $"Use 1 to {TaskTags.MaxLength} of the characters a-z, 0-9 and : + # . -");
            }
            else if (!normalised.Contains(tag, StringComparer.Ordinal))
            {
                normalised.Add(tag);
            }
        }

        if (normalised.Count > TaskTags.MaxCount)
        {
            Add(errors, "tags", $"Use at most {TaskTags.MaxCount} tags.");
        }

        return normalised;
    }

    private static void ValidateRubric(Rubric? rubric, Dictionary<string, List<string>> errors)
    {
        if (rubric?.Criteria is not { Count: > 0 } criteria)
        {
            Add(errors, "rubric", "Add at least one criterion.");
            return;
        }

        if (!criteria.OfType<ScaleCriterion>().Any())
        {
            Add(errors, "rubric", "Add at least one scale criterion.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < criteria.Count; index++)
        {
            var field = $"rubric.criteria[{index}]";
            var criterion = criteria[index];
            if (criterion is null)
            {
                Add(errors, field, "Define the criterion.");
                continue;
            }

            if (!KebabCase.IsMatch(criterion.Id))
            {
                Add(errors, $"{field}.id", "Use a kebab-case id, for example 'error-handling'.");
            }
            else if (!ids.Add(criterion.Id))
            {
                Add(errors, $"{field}.id", "Another criterion already uses this id.");
            }

            if (string.IsNullOrWhiteSpace(criterion.Title))
            {
                Add(errors, $"{field}.title", "Enter a title.");
            }

            switch (criterion)
            {
                case ScaleCriterion { Weight: < MinScaleWeight or > MaxScaleWeight }:
                    Add(errors, $"{field}.weight", $"Use a weight between {MinScaleWeight} and {MaxScaleWeight}.");
                    break;
                case CheckCriterion check when !Enum.IsDefined(check.Expected):
                    Add(errors, $"{field}.expected", "Choose the expected answer.");
                    break;
            }
        }
    }

    private static bool ValidateContentSize(TaskVersionDetails details, Dictionary<string, List<string>> errors)
    {
        var candidate = SizeOf(details.CandidateContent, TaskType<object, object, object>.CandidateContentField, errors);
        var staff = SizeOf(details.StaffContent, TaskType<object, object, object>.StaffContentField, errors);
        if (candidate is null || staff is null)
        {
            return false;
        }

        if (candidate + staff > TaskVersionDetails.ContentMaxBytes)
        {
            Add(errors, "content", $"The content is larger than {TaskVersionDetails.ContentMaxBytes / 1024 / 1024} MB.");
            return false;
        }

        return true;
    }

    private static long? SizeOf(JsonElement content, string field, Dictionary<string, List<string>> errors)
    {
        if (content.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            Add(errors, field, "It is required.");
            return null;
        }

        return JsonSerializer.SerializeToUtf8Bytes(content).LongLength;
    }

    private static void AddTaskTypeErrors(Result result, Dictionary<string, List<string>> errors)
    {
        switch (result.Error)
        {
            case null:
                return;
            case ValidationError validation:
                foreach (var (field, messages) in validation.Errors)
                {
                    foreach (var message in messages)
                    {
                        Add(errors, field, message);
                    }
                }

                return;
            default:
                Add(errors, "content", result.Error.Description);
                return;
        }
    }

    private static string HashOf(ITaskType taskType, TaskVersionDetails details)
    {
        var hashed = new HashedVersion(
            taskType.Key,
            taskType.ContentSchemaVersion,
            details.Title,
            details.Summary,
            details.Level!.Value.ToString(),
            details.EstimatedMinutes,
            details.Tags!,
            details.ReferenceAnswer,
            details.InterviewerNotes,
            details.Rubric!,
            details.CandidateContent,
            details.StaffContent);

        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(hashed, TaskTypeJson.Options)));
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            errors[field] = messages = [];
        }

        messages.Add(message);
    }

    private sealed record HashedVersion(
        string TaskType,
        int ContentSchemaVersion,
        string Title,
        string Summary,
        string Level,
        int EstimatedMinutes,
        IReadOnlyList<string> Tags,
        string ReferenceAnswer,
        string? InterviewerNotes,
        Rubric Rubric,
        JsonElement CandidateContent,
        JsonElement StaffContent);
}
