using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.TaskTypes.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.TaskTypes.OpenQuestion.Tests;

public sealed class OpenQuestionTaskTypeTests
{
    private static readonly ITaskType _openQuestion = new OpenQuestionTaskType();

    [Fact]
    public void The_catalog_ships_the_open_question_type()
    {
        // Act
        var registry = new ServiceCollection().AddTaskTypes().BuildServiceProvider().GetRequiredService<ITaskTypeRegistry>();

        // Assert
        registry.Find("open-question").ShouldBeOfType<OpenQuestionTaskType>();
    }

    [Fact]
    public void A_markdown_prompt_with_empty_staff_content_is_valid()
    {
        // Act
        var result = _openQuestion.ValidateContent(Json("""{ "prompt": "## Fake question\n\nExplain **made-up** things." }"""), Json("{}"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("""{ "prompt": "" }""")]
    [InlineData("""{ "prompt": "  \n " }""")]
    public void A_blank_prompt_is_refused(string candidateContent)
    {
        // Act
        var result = _openQuestion.ValidateContent(Json(candidateContent), Json("{}"));

        // Assert
        ErrorsOf(result)["candidateContent.prompt"].ShouldBe(["Enter a prompt."]);
    }

    [Fact]
    public void A_prompt_over_the_limit_is_refused()
    {
        // Act
        var result = _openQuestion.ValidateContent(Prompt(new string('a', OpenQuestionTaskType.PromptMaxLength + 1)), Json("{}"));

        // Assert
        ErrorsOf(result)["candidateContent.prompt"].ShouldBe(["Keep the prompt to at most 20,000 characters."]);
    }

    [Fact]
    public void A_prompt_at_the_limit_is_valid()
    {
        // Act
        var result = _openQuestion.ValidateContent(Prompt(new string('a', OpenQuestionTaskType.PromptMaxLength)), Json("{}"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("{}", "{}")]
    [InlineData("""{ "prompt": null }""", "{}")]
    [InlineData("""{ "prompt": "Fake?", "hint": "Made up" }""", "{}")]
    public void Candidate_content_not_matching_the_model_is_refused(string candidateContent, string staffContent)
    {
        // Act
        var result = _openQuestion.ValidateContent(Json(candidateContent), Json(staffContent));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent"]);
    }

    [Theory]
    [InlineData("""{ "expectedAnswer": "Fake" }""")]
    [InlineData("null")]
    public void Staff_content_other_than_an_empty_object_is_refused(string staffContent)
    {
        // Act
        var result = _openQuestion.ValidateContent(Json("""{ "prompt": "Fake?" }"""), Json(staffContent));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["staffContent"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A made-up answer with `code`.")]
    public void A_markdown_answer_is_valid(string answer)
    {
        // Act
        var result = _openQuestion.ValidateResponse(Answer(answer));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void An_answer_at_the_limit_is_valid()
    {
        // Act
        var result = _openQuestion.ValidateResponse(Answer(new string('a', OpenQuestionTaskType.AnswerMaxLength)));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void An_answer_over_the_limit_is_refused()
    {
        // Act
        var result = _openQuestion.ValidateResponse(Answer(new string('a', OpenQuestionTaskType.AnswerMaxLength + 1)));

        // Assert
        ErrorsOf(result)["response.answer"].ShouldBe(["Keep the answer to at most 20,000 characters."]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "answer": null }""")]
    [InlineData("""{ "answer": 42 }""")]
    [InlineData("""{ "answer": "Fake", "draft": true }""")]
    public void A_response_not_matching_the_model_is_refused(string response)
    {
        // Act
        var result = _openQuestion.ValidateResponse(Json(response));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response"]);
    }

    [Fact]
    public void No_scores_are_suggested()
    {
        // Arrange
        var rubric = new Rubric(
        [
            new ScaleCriterion("fake-clarity", "Fake clarity", "Explains the made-up idea.", Weight: 2),
            new CheckCriterion("fake-example", "Fake example", "Gives an example.", CheckAnswer.Yes),
        ]);

        // Act
        var suggestions = _openQuestion.SuggestScores(Json("""{ "prompt": "Fake?" }"""), Json("{}"), rubric, Answer("A made-up answer."));

        // Assert
        suggestions.ShouldBeEmpty();
    }

    private static JsonElement Prompt(string prompt) => JsonSerializer.SerializeToElement(new { prompt });

    private static JsonElement Answer(string answer) => JsonSerializer.SerializeToElement(new { answer });

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
