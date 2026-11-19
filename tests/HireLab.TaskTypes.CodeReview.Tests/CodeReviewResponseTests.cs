using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;
using static HireLab.TaskTypes.CodeReview.Tests.CodeReviewBuilders;

namespace HireLab.TaskTypes.CodeReview.Tests;

public sealed class CodeReviewResponseTests
{
    private static readonly ITaskType _codeReview = new CodeReviewTaskType();

    [Fact]
    public void A_review_with_comments_a_summary_and_a_verdict_is_valid()
    {
        // Act
        var result = Validate(Response);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void An_empty_review_without_a_verdict_is_valid()
    {
        // Act
        var result = _codeReview.ValidateResponse(Json("""{ "comments": [], "summary": "" }"""));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Approve")]
    [InlineData("RequestChanges")]
    [InlineData("Comment")]
    public void Every_verdict_is_valid(string verdict)
    {
        // Act
        var result = _codeReview.ValidateResponse(Json($$"""{ "comments": [], "summary": "", "verdict": "{{verdict}}" }"""));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_response_at_every_limit_is_valid()
    {
        // Arrange
        var comments = Enumerable.Range(1, CodeReviewTaskType.MaxComments)
            .Select(number => CommentNumbered(number) with
            {
                File = new string('f', CodeReviewTaskType.PathMaxLength),
                Body = new string('b', CodeReviewTaskType.BodyMaxLength),
            })
            .ToList();

        // Act
        var result = Validate(new CodeReviewResponse(comments, new string('s', CodeReviewTaskType.SummaryMaxLength), ReviewVerdict.Approve));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_comment_on_a_file_or_lines_the_task_does_not_have_is_valid()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { File = "src/Fake/NotInTheTask.cs", StartLine = 900, EndLine = 990 }] });

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void More_than_200_comments_are_refused()
    {
        // Arrange
        var comments = Enumerable.Range(1, CodeReviewTaskType.MaxComments + 1).Select(CommentNumbered).ToList();

        // Act
        var result = Validate(Response with { Comments = comments });

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response.comments"]);
        ErrorsOf(result)["response.comments"].ShouldBe(["Add at most 200 review comments."]);
    }

    [Fact]
    public void A_comment_id_used_twice_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment, Comment with { Body = "Fake second thought." }] });

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response.comments[1].id"]);
        ErrorsOf(result)["response.comments[1].id"].ShouldBe(["Another review comment already uses this id."]);
    }

    [Fact]
    public void An_empty_comment_id_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { Id = Guid.Empty }] });

        // Assert
        ErrorsOf(result)["response.comments[0].id"].ShouldBe(["Use a non-empty id."]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_comment_without_a_file_is_refused(string file)
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { File = file }] });

        // Assert
        ErrorsOf(result)["response.comments[0].file"].ShouldBe(["Enter the path of the file."]);
    }

    [Fact]
    public void A_comment_file_over_the_path_limit_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { File = new string('f', CodeReviewTaskType.PathMaxLength + 1) }] });

        // Assert
        ErrorsOf(result)["response.comments[0].file"].ShouldBe(["Keep the path to at most 300 characters."]);
    }

    [Fact]
    public void A_comment_on_an_undefined_side_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { Side = (DiffSide)5 }] });

        // Assert
        ErrorsOf(result)["response.comments[0].side"].ShouldBe(["Choose the original or the modified side."]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 4)]
    public void A_comment_starting_before_line_1_is_refused(int startLine, int endLine)
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { StartLine = startLine, EndLine = endLine }] });

        // Assert
        ErrorsOf(result)["response.comments[0].startLine"].ShouldBe(["Start at line 1 or later."]);
    }

    [Fact]
    public void A_comment_ending_before_it_starts_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { StartLine = 7, EndLine = 6 }] });

        // Assert
        ErrorsOf(result)["response.comments[0].endLine"].ShouldBe(["End at the start line or later."]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n ")]
    public void A_blank_comment_body_is_refused(string body)
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { Body = body }] });

        // Assert
        ErrorsOf(result)["response.comments[0].body"].ShouldBe(["Enter a comment."]);
    }

    [Fact]
    public void A_comment_body_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(Response with { Comments = [Comment with { Body = new string('b', CodeReviewTaskType.BodyMaxLength + 1) }] });

        // Assert
        ErrorsOf(result)["response.comments[0].body"].ShouldBe(["Keep the comment to at most 5,000 characters."]);
    }

    [Fact]
    public void A_summary_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(Response with { Summary = new string('s', CodeReviewTaskType.SummaryMaxLength + 1) });

        // Assert
        ErrorsOf(result)["response.summary"].ShouldBe(["Keep the summary to at most 20,000 characters."]);
    }

    [Fact]
    public void An_undefined_verdict_is_refused()
    {
        // Act
        var result = Validate(Response with { Verdict = (ReviewVerdict)8 });

        // Assert
        ErrorsOf(result)["response.verdict"].ShouldBe(["Choose Approve, Request changes or Comment."]);
    }

    [Fact]
    public void A_missing_comment_entry_is_refused()
    {
        // Act
        var result = _codeReview.ValidateResponse(Json("""{ "comments": [null], "summary": "" }"""));

        // Assert
        ErrorsOf(result)["response.comments[0]"].ShouldBe(["Define the review comment."]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "comments": [] }""")]
    [InlineData("""{ "comments": [], "summary": "", "draft": true }""")]
    [InlineData("""{ "comments": [], "summary": "", "verdict": "Reject" }""")]
    [InlineData("""{ "comments": [{ "id": "not-a-guid", "file": "fake.cs", "side": "modified", "startLine": 1, "endLine": 1, "body": "Fake" }], "summary": "" }""")]
    [InlineData("""{ "comments": [{ "id": "0196f0c4-0000-7000-8000-000000000001", "file": "fake.cs", "side": "right", "startLine": 1, "endLine": 1, "body": "Fake" }], "summary": "" }""")]
    [InlineData("""{ "comments": [{ "id": "0196f0c4-0000-7000-8000-000000000001", "file": "fake.cs", "side": "modified", "startLine": 1, "body": "Fake" }], "summary": "" }""")]
    public void A_response_not_matching_the_model_is_refused(string response)
    {
        // Act
        var result = _codeReview.ValidateResponse(Json(response));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["response"]);
    }

    [Fact]
    public void The_response_uses_the_documented_wire_values()
    {
        // Act
        var json = ToJson(Response);

        // Assert
        json.GetProperty("verdict").GetString().ShouldBe("RequestChanges");
        var comment = json.GetProperty("comments")[0];
        comment.GetProperty("side").GetString().ShouldBe("modified");
        comment.GetProperty("startLine").GetInt32().ShouldBe(3);
    }

    [Fact]
    public void No_scores_are_suggested()
    {
        // Arrange
        var rubric = new Rubric(
        [
            new ScaleCriterion("fake-thoroughness", "Fake thoroughness", "Finds the made-up issues.", Weight: 3),
            new CheckCriterion("fake-null-check", "Fake null check", "Spots the missing null check.", CheckAnswer.Yes),
        ]);

        // Act
        var suggestions = _codeReview.SuggestScores(ToJson(Candidate), ToJson(Staff), rubric, ToJson(Response));

        // Assert
        suggestions.ShouldBeEmpty();
    }

    private static Result Validate(CodeReviewResponse response) => _codeReview.ValidateResponse(ToJson(response));
}
