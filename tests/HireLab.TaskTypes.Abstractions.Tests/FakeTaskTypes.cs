using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions.Tests;

internal sealed record QuizCandidateContent(string Question);

internal sealed record QuizStaffContent(string ExpectedAnswer, string? CriterionId = null);

internal sealed record QuizResponse(string Answer);

internal class QuizTaskType : TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>
{
    public const string QuizKey = "quiz";

    public override string Key => QuizKey;

    public override int ContentSchemaVersion => 1;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<QuizCandidateContent, QuizStaffContent> content) =>
        string.IsNullOrWhiteSpace(content.Candidate.Question)
            ? new ValidationError(new Dictionary<string, string[]> { ["candidateContent.question"] = ["Enter a question."] })
            : Result.Success();

    public override Result ValidateResponse(QuizResponse response) =>
        response.Answer.Length > 100
            ? new ValidationError(new Dictionary<string, string[]> { ["response.answer"] = ["Keep the answer short."] })
            : Result.Success();

    public override IReadOnlyList<CriterionSuggestion> SuggestScores(
        TaskContent<QuizCandidateContent, QuizStaffContent> content,
        Rubric rubric,
        QuizResponse response) =>
        rubric.Criteria.OfType<CheckCriterion>()
            .Where(criterion => criterion.Id == content.Staff.CriterionId)
            .Select(criterion => new CheckSuggestion(
                criterion.Id,
                response.Answer == content.Staff.ExpectedAnswer ? CheckAnswer.Yes : CheckAnswer.No,
                "Compared with the expected answer."))
            .ToList<CriterionSuggestion>();
}

internal sealed class SecondQuizTaskType : QuizTaskType;

internal sealed class PollTaskType : TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>
{
    public override string Key => "poll";

    public override int ContentSchemaVersion => 2;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<QuizCandidateContent, QuizStaffContent> content) => Result.Success();

    public override Result ValidateResponse(QuizResponse response) => Result.Success();
}

internal sealed class BadlyKeyedTaskType : TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>
{
    public override string Key => "Bad Key";

    public override int ContentSchemaVersion => 1;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<QuizCandidateContent, QuizStaffContent> content) => Result.Success();

    public override Result ValidateResponse(QuizResponse response) => Result.Success();
}

internal sealed class UnversionedTaskType : TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>
{
    public override string Key => "unversioned";

    public override int ContentSchemaVersion => 0;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<QuizCandidateContent, QuizStaffContent> content) => Result.Success();

    public override Result ValidateResponse(QuizResponse response) => Result.Success();
}
