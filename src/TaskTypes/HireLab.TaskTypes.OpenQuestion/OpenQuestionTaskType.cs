using System.Globalization;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.TaskTypes.OpenQuestion;

/// <summary>
/// A question the candidate answers in Markdown. The candidate content is the Markdown prompt, the staff content is
/// empty and the response is the Markdown answer. It suggests no criterion scores.
/// </summary>
public sealed class OpenQuestionTaskType : TaskType<OpenQuestionCandidateContent, OpenQuestionStaffContent, OpenQuestionResponse>
{
    public const string TaskTypeKey = "open-question";

    public const int PromptMaxLength = 20_000;

    public const int AnswerMaxLength = 20_000;

    public override string Key => TaskTypeKey;

    public override int ContentSchemaVersion => 1;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<OpenQuestionCandidateContent, OpenQuestionStaffContent> content)
    {
        var prompt = content.Candidate.Prompt;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Error("candidateContent.prompt", "Enter a prompt.");
        }

        return prompt.Length > PromptMaxLength
            ? Error("candidateContent.prompt", string.Create(CultureInfo.InvariantCulture, $"Keep the prompt to at most {PromptMaxLength:N0} characters."))
            : Result.Success();
    }

    public override Result ValidateResponse(OpenQuestionResponse response) =>
        response.Answer.Length > AnswerMaxLength
            ? Error("response.answer", string.Create(CultureInfo.InvariantCulture, $"Keep the answer to at most {AnswerMaxLength:N0} characters."))
            : Result.Success();

    private static ValidationError Error(string field, string message) =>
        new(new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] });
}

/// <summary>What the candidate sees: the question, in Markdown.</summary>
public sealed record OpenQuestionCandidateContent(string Prompt);

/// <summary>An open question has no staff-only content; the reference answer and notes cover what staff need.</summary>
public sealed record OpenQuestionStaffContent;

/// <summary>The candidate's answer, in Markdown. It may be empty while the candidate is still writing.</summary>
public sealed record OpenQuestionResponse(string Answer);
