using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Testing.TaskTypes;

/// <summary>
/// A fake task type that proves the task type framework without a real one (ADR 0028). The candidate repeats a
/// phrase; a check criterion named in the staff content is suggested from whether the echo matches. Its candidate
/// endpoint echoes text back. Registered in every test host and never in the real API.
/// </summary>
public sealed class TestEchoTaskType : TaskType<TestEchoCandidateContent, TestEchoStaffContent, TestEchoResponse>, ITaskTypeEndpoints
{
    public const string TaskTypeKey = "test-echo";

    public const string EchoPath = "/api/candidate/tasks/test-echo/echo";

    public override string Key => TaskTypeKey;

    public override int ContentSchemaVersion => 1;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<TestEchoCandidateContent, TestEchoStaffContent> content) =>
        string.IsNullOrWhiteSpace(content.Candidate.Phrase)
            ? new ValidationError(new Dictionary<string, string[]> { ["candidateContent.phrase"] = ["Enter a phrase."] })
            : Result.Success();

    public override Result ValidateResponse(TestEchoResponse response) => Result.Success();

    public override IReadOnlyList<CriterionSuggestion> SuggestScores(
        TaskContent<TestEchoCandidateContent, TestEchoStaffContent> content,
        Rubric rubric,
        TestEchoResponse response) =>
        rubric.Criteria.OfType<CheckCriterion>()
            .Where(criterion => criterion.Id == content.Staff.MatchCriterionId)
            .Select(criterion => new CheckSuggestion(
                criterion.Id,
                response.Echo == content.Candidate.Phrase ? CheckAnswer.Yes : CheckAnswer.No,
                "Compared the echo with the phrase."))
            .ToList<CriterionSuggestion>();

    public void MapCandidateEndpoints(IEndpointRouteBuilder taskType) =>
        taskType.MapGet("/echo", (string text) => TypedResults.Ok(new TestEchoReply(text)));
}

public sealed record TestEchoCandidateContent(string Phrase);

public sealed record TestEchoStaffContent(string MatchCriterionId);

public sealed record TestEchoResponse(string Echo);

public sealed record TestEchoReply(string Text);
