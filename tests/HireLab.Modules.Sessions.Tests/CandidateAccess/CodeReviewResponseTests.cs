using System.Net;
using System.Net.Http.Json;
using HireLab.Modules.Sessions.Tests.Sessions;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.CodeReview;
using HireLab.Testing.Positions;
using HireLab.Testing.Sessions;
using HireLab.Testing.TaskBank;
using Microsoft.AspNetCore.Http;

namespace HireLab.Modules.Sessions.Tests.CandidateAccess;

public sealed class CodeReviewResponseTests(HireLabTestHost host) : IntegrationTest(host)
{
    private const string FakePath = "src/FakeShop/FakeOrderHandler.cs";

    private static readonly CodeReviewCandidateContent _candidateContent = new(
        "Fake: save orders in the handler",
        "Saves a **made-up** order.",
        [new ChangedFile(FakePath, "csharp", "public class FakeOrderHandler { }\n", "public class FakeOrderHandler\n{\n    public void Handle(FakeOrder order) => order.Save();\n}\n")]);

    private static readonly CodeReviewStaffContent _staffContent = new(
    [
        new ExpectedFinding("fake-missing-null-check", FakePath, DiffSide.Modified, 3, 3, "Fake missing null check", "The made-up order may be null.", FindingSeverity.Major),
    ]);

    [Fact]
    public async Task A_valid_review_is_saved_with_the_documented_wire_values()
    {
        // Arrange
        var session = await LiveCodeReviewSessionAsync();
        var commentId = Guid.CreateVersion7();
        var review = new CodeReviewResponse(
            [new ReviewComment(commentId, FakePath, DiffSide.Modified, 3, 3, "What if the fake `order` is null?")],
            "A made-up summary.",
            ReviewVerdict.RequestChanges);

        // Act
        var revision = await session.Candidate.SavedResponseAsync(session.First, review, 0);

        // Assert
        revision.ShouldBe(1);
        var stored = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Response.ShouldNotBeNull();
        stored.GetProperty("verdict").GetString().ShouldBe("RequestChanges");
        var comment = stored.GetProperty("comments")[0];
        comment.GetProperty("id").GetGuid().ShouldBe(commentId);
        comment.GetProperty("side").GetString().ShouldBe("modified");
    }

    [Fact]
    public async Task A_review_comment_on_lines_the_task_does_not_have_is_saved()
    {
        // Arrange
        var session = await LiveCodeReviewSessionAsync();
        var review = new { comments = new[] { Comment(file: "src/Fake/NotInTheTask.cs", startLine: 500, endLine: 510) }, summary = "" };

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, review, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_invalid_review_is_refused_with_the_field_key()
    {
        // Arrange
        var session = await LiveCodeReviewSessionAsync();
        var review = new { comments = new[] { Comment(), Comment(startLine: 4, endLine: 2, body: " ") }, summary = "", verdict = "Approve" };

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, review, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        problem.Errors.Keys.ShouldBe(["response.comments[1].endLine", "response.comments[1].body"], ignoreOrder: true);
        (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull().Revision.ShouldBe(0);
    }

    [Fact]
    public async Task A_review_not_matching_the_model_is_refused()
    {
        // Arrange
        var session = await LiveCodeReviewSessionAsync();
        var review = new { comments = Array.Empty<object>(), summary = "", verdict = "Reject" };

        // Act
        using var response = await session.Candidate.SaveResponseAsync(session.First, review, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        problem.Errors.Keys.ShouldBe(["response"]);
    }

    [Fact]
    public async Task The_candidate_sees_the_pull_request_but_not_the_expected_findings()
    {
        // Arrange
        var session = await LiveCodeReviewSessionAsync();

        // Act
        var current = (await session.Candidate.InterviewAsync()).CurrentTask.ShouldNotBeNull();

        // Assert
        current.TaskType.ShouldBe(CodeReviewTaskType.TaskTypeKey);
        current.Content.GetProperty("files")[0].GetProperty("path").GetString().ShouldBe(FakePath);
        current.Content.GetRawText().ShouldNotContain("fake-missing-null-check");
    }

    private static object Comment(string file = FakePath, int startLine = 3, int endLine = 3, string body = "A made-up remark.") =>
        new { id = Guid.CreateVersion7(), file, side = "modified", startLine, endLine, body };

    private async Task<CodeReviewSession> LiveCodeReviewSessionAsync()
    {
        var staff = await SignInAsAsync(StaffRole.Interviewer);
        var teamId = staff.User.TeamId!.Value;
        var browser = await CreateBrowserAsync(staff);
        var created = await browser.CreatedAsync(await Host.CreateCandidateAsync(teamId));
        var taskId = await Host.AddBankTaskAsync(teamId, CodeReviewTaskType.TaskTypeKey, _candidateContent, _staffContent);
        var first = await browser.AddedTaskAsync(created.SessionId, taskId);
        var token = await browser.IssuedCandidateLinkAsync(created.Id);
        await browser.WentLiveAsync(created.SessionId);
        var candidate = await Host.CreateCandidateBrowserAsync();
        await candidate.ExchangedAsync(token);
        return new CodeReviewSession(candidate, first);
    }

    private sealed record CodeReviewSession(BrowserSession Candidate, Guid First);
}
