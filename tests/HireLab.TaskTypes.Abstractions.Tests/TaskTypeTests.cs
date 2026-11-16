using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions.Tests;

public sealed class TaskTypeTests
{
    private static readonly ITaskType _quiz = new QuizTaskType();

    [Fact]
    public void Task_type_exposes_its_models()
    {
        // Assert
        _quiz.CandidateContentType.ShouldBe(typeof(QuizCandidateContent));
        _quiz.StaffContentType.ShouldBe(typeof(QuizStaffContent));
        _quiz.ResponseType.ShouldBe(typeof(QuizResponse));
    }

    [Fact]
    public void Content_matching_the_models_and_the_rules_is_valid()
    {
        // Act
        var result = _quiz.ValidateContent(Json("""{ "question": "What is 2 + 2?" }"""), Json("""{ "expectedAnswer": "4" }"""));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Content_breaking_the_type_rules_fails_with_the_type_errors()
    {
        // Act
        var result = _quiz.ValidateContent(Json("""{ "question": " " }"""), Json("""{ "expectedAnswer": "4" }"""));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent.question"]);
    }

    [Fact]
    public void Content_not_matching_the_models_fails_for_each_part()
    {
        // Act
        var result = _quiz.ValidateContent(Json("""{ "question": 4 }"""), Json("""{ "expectedAnswer": "4", "extra": true }"""));

        // Assert
        var errors = ErrorsOf(result);
        errors.Keys.ShouldBe([TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>.CandidateContentField,
            TaskType<QuizCandidateContent, QuizStaffContent, QuizResponse>.StaffContentField], ignoreOrder: true);
        errors["candidateContent"].ShouldHaveSingleItem().ShouldContain("$.question");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{ "question": null }""")]
    public void Missing_content_fails(string candidateContent)
    {
        // Act
        var result = _quiz.ValidateContent(Json(candidateContent), Json("""{ "expectedAnswer": "4" }"""));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent"]);
    }

    [Fact]
    public void Response_matching_the_model_and_the_rules_is_valid()
    {
        // Act
        var result = _quiz.ValidateResponse(Json("""{ "answer": "4" }"""));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Response_breaking_the_type_rules_fails_with_the_type_errors()
    {
        // Act
        var result = _quiz.ValidateResponse(Json($$"""{ "answer": "{{new string('a', 101)}}" }"""));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response.answer"]);
    }

    [Fact]
    public void Response_not_matching_the_model_fails()
    {
        // Act
        var result = _quiz.ValidateResponse(Json("""{ "answer": "4", "score": 10 }"""));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response"]);
    }

    [Fact]
    public void Scores_are_suggested_from_the_stored_json()
    {
        // Arrange
        var rubric = new Rubric(
        [
            new ScaleCriterion("clarity", "Clarity", "Explains the answer.", Weight: 2),
            new CheckCriterion("correct", "Correct", "Gives the expected answer.", CheckAnswer.Yes, Critical: true),
        ]);

        // Act
        var suggestions = _quiz.SuggestScores(
            Json("""{ "question": "What is 2 + 2?" }"""),
            Json("""{ "expectedAnswer": "4", "criterionId": "correct" }"""),
            rubric,
            Json("""{ "answer": "4" }"""));

        // Assert
        suggestions.ShouldHaveSingleItem().ShouldBe(new CheckSuggestion("correct", CheckAnswer.Yes, "Compared with the expected answer."));
    }

    [Fact]
    public void A_task_type_suggests_nothing_by_default()
    {
        // Act
        var suggestions = ((ITaskType)new PollTaskType()).SuggestScores(
            Json("""{ "question": "Tabs or spaces?" }"""),
            Json("""{ "expectedAnswer": "spaces" }"""),
            new Rubric([]),
            Json("""{ "answer": "tabs" }"""));

        // Assert
        suggestions.ShouldBeEmpty();
    }

    [Fact]
    public void Rubrics_and_suggestions_round_trip_with_type_discriminators()
    {
        // Arrange
        var rubric = new Rubric(
        [
            new ScaleCriterion("design", "Design", "Separates concerns.", 3, [new ScaleLevel(4, "Clean layers.")]),
            new CheckCriterion("no-leak", "No data leak", "Pastes no secrets.", CheckAnswer.No, Critical: true),
        ]);
        CriterionSuggestion suggestion = new ScaleSuggestion("design", 3, "Most layers are clean.");

        // Act
        var rubricJson = JsonSerializer.Serialize(rubric, TaskTypeJson.Options);
        var suggestionJson = JsonSerializer.Serialize(suggestion, TaskTypeJson.Options);

        // Assert
        rubricJson.ShouldContain("\"type\":\"scale\"");
        rubricJson.ShouldContain("\"expected\":\"no\"");
        var readRubric = JsonSerializer.Deserialize<Rubric>(rubricJson, TaskTypeJson.Options);
        readRubric!.Criteria[1].ShouldBe(rubric.Criteria[1]);
        JsonSerializer.Serialize(readRubric, TaskTypeJson.Options).ShouldBe(rubricJson);
        JsonSerializer.Deserialize<CriterionSuggestion>(suggestionJson, TaskTypeJson.Options).ShouldBe(suggestion);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void A_scale_suggestion_outside_0_to_4_is_refused(int score)
    {
        // Act
        var create = () => new ScaleSuggestion("design", score, "Out of range.");

        // Assert
        create.ShouldThrow<ArgumentOutOfRangeException>();
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static IReadOnlyDictionary<string, string[]> ErrorsOf(Result result)
    {
        result.IsFailure.ShouldBeTrue();
        return result.Error.ShouldBeOfType<ValidationError>().Errors;
    }
}
