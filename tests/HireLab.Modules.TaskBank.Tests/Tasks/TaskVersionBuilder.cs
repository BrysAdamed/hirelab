using System.Text.Json;
using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Tests.Tasks;

/// <summary>Builds valid <c>test-echo</c> task version details with obviously fake values.</summary>
internal sealed class TaskVersionBuilder
{
    private static int _sequence;

    private readonly int _number = Interlocked.Increment(ref _sequence);
    private string? _title;
    private string _summary = "A made-up task for tests.";
    private TaskLevel? _level = TaskLevel.Mid;
    private int _estimatedMinutes = 30;
    private IReadOnlyList<string>? _tags = ["fake", "testing"];
    private string _referenceAnswer = "A made-up reference answer.";
    private string? _interviewerNotes;
    private Rubric? _rubric = new(
    [
        new ScaleCriterion("fake-quality", "Fake quality", "How convincingly fake it is.", Weight: 2),
        new CheckCriterion("fake-echo", "Fake echo", "Did the candidate echo the phrase?", CheckAnswer.Yes, Critical: true),
    ]);

    private JsonElement _candidateContent = Json("""{ "phrase": "Hello, fake world" }""");
    private JsonElement _staffContent = Json("""{ "matchCriterionId": "fake-echo" }""");

    public static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    public TaskVersionBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TaskVersionBuilder WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    public TaskVersionBuilder WithLevel(TaskLevel? level)
    {
        _level = level;
        return this;
    }

    public TaskVersionBuilder WithEstimatedMinutes(int estimatedMinutes)
    {
        _estimatedMinutes = estimatedMinutes;
        return this;
    }

    public TaskVersionBuilder WithTags(params string[]? tags)
    {
        _tags = tags;
        return this;
    }

    public TaskVersionBuilder WithReferenceAnswer(string referenceAnswer)
    {
        _referenceAnswer = referenceAnswer;
        return this;
    }

    public TaskVersionBuilder WithInterviewerNotes(string? interviewerNotes)
    {
        _interviewerNotes = interviewerNotes;
        return this;
    }

    public TaskVersionBuilder WithRubric(params RubricCriterion[]? criteria)
    {
        _rubric = criteria is null ? null : new Rubric(criteria);
        return this;
    }

    public TaskVersionBuilder WithPhrase(string phrase)
    {
        _candidateContent = JsonSerializer.SerializeToElement(new { phrase });
        return this;
    }

    public TaskVersionBuilder WithCandidateContent(JsonElement candidateContent)
    {
        _candidateContent = candidateContent;
        return this;
    }

    public TaskVersionBuilder WithStaffContent(JsonElement staffContent)
    {
        _staffContent = staffContent;
        return this;
    }

    public TaskVersionDetails Build() => new(
        _title ?? $"Fake Task {_number}",
        _summary,
        _level,
        _estimatedMinutes,
        _tags,
        _referenceAnswer,
        _interviewerNotes,
        _rubric,
        _candidateContent,
        _staffContent);
}
