using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Tests.Tasks;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.TaskBank.Tests.Domain;

public sealed class TaskVersionValidatorTests
{
    private static readonly ScaleCriterion _scale = new("fake-quality", "Fake quality", "How fake it is.", Weight: 3);

    private static readonly Dictionary<string, (Func<TaskVersionBuilder> Build, string Field)> _invalid = new()
    {
        ["blank title"] = (() => new TaskVersionBuilder().WithTitle("   "), "title"),
        ["title over 120 characters"] = (() => new TaskVersionBuilder().WithTitle(new string('t', TaskVersionDetails.TitleMaxLength + 1)), "title"),
        ["blank summary"] = (() => new TaskVersionBuilder().WithSummary(string.Empty), "summary"),
        ["summary over 1000 characters"] = (() => new TaskVersionBuilder().WithSummary(new string('s', TaskVersionDetails.SummaryMaxLength + 1)), "summary"),
        ["no level"] = (() => new TaskVersionBuilder().WithLevel(null), "level"),
        ["unknown level"] = (() => new TaskVersionBuilder().WithLevel((TaskLevel)99), "level"),
        ["zero minutes"] = (() => new TaskVersionBuilder().WithEstimatedMinutes(0), "estimatedMinutes"),
        ["over 180 minutes"] = (() => new TaskVersionBuilder().WithEstimatedMinutes(TaskVersionDetails.MaxEstimatedMinutes + 1), "estimatedMinutes"),
        ["tag with a space"] = (() => new TaskVersionBuilder().WithTags("fake", "two words"), "tags[1]"),
        ["tag with an underscore"] = (() => new TaskVersionBuilder().WithTags("snake_case"), "tags[0]"),
        ["blank tag"] = (() => new TaskVersionBuilder().WithTags("  "), "tags[0]"),
        ["tag over 32 characters"] = (() => new TaskVersionBuilder().WithTags(new string('x', TaskTags.MaxLength + 1)), "tags[0]"),
        ["11 tags"] = (() => new TaskVersionBuilder().WithTags([.. Enumerable.Range(1, TaskTags.MaxCount + 1).Select(index => $"tag-{index}")]), "tags"),
        ["blank reference answer"] = (() => new TaskVersionBuilder().WithReferenceAnswer(" "), "referenceAnswer"),
        ["no rubric"] = (() => new TaskVersionBuilder().WithRubric(null), "rubric"),
        ["rubric without criteria"] = (() => new TaskVersionBuilder().WithRubric(), "rubric"),
        ["rubric without a scale criterion"] = (
            () => new TaskVersionBuilder().WithRubric(new CheckCriterion("fake-check", "Fake check", "Fake.", CheckAnswer.No)),
            "rubric"),
        ["criterion id not in kebab case"] = (() => new TaskVersionBuilder().WithRubric(_scale with { Id = "Fake_Quality" }), "rubric.criteria[0].id"),
        ["duplicate criterion id"] = (
            () => new TaskVersionBuilder().WithRubric(_scale, new CheckCriterion(_scale.Id, "Fake check", "Fake.", CheckAnswer.Yes)),
            "rubric.criteria[1].id"),
        ["blank criterion title"] = (() => new TaskVersionBuilder().WithRubric(_scale with { Title = " " }), "rubric.criteria[0].title"),
        ["zero weight"] = (() => new TaskVersionBuilder().WithRubric(_scale with { Weight = 0 }), "rubric.criteria[0].weight"),
        ["weight above 10"] = (() => new TaskVersionBuilder().WithRubric(_scale with { Weight = 11 }), "rubric.criteria[0].weight"),
        ["check without an expected answer"] = (
            () => new TaskVersionBuilder().WithRubric(_scale, new CheckCriterion("fake-check", "Fake check", "Fake.", (CheckAnswer)7)),
            "rubric.criteria[1].expected"),
        ["no candidate content"] = (() => new TaskVersionBuilder().WithCandidateContent(default), "candidateContent"),
        ["null staff content"] = (() => new TaskVersionBuilder().WithStaffContent(TaskVersionBuilder.Json("null")), "staffContent"),
        ["content over 1 MB"] = (() => new TaskVersionBuilder().WithPhrase(new string('f', TaskVersionDetails.ContentMaxBytes)), "content"),
        ["content not matching the type's model"] = (
            () => new TaskVersionBuilder().WithCandidateContent(TaskVersionBuilder.Json("""{ "phrase": "Fake", "typo": true }""")),
            "candidateContent"),
        ["content breaking the type's rule"] = (() => new TaskVersionBuilder().WithPhrase(" "), "candidateContent.phrase"),
    };

    public static TheoryData<string> InvalidDetails => new(_invalid.Keys);

    [Fact]
    public void Valid_details_pass_with_the_types_schema_version_and_a_content_hash()
    {
        // Act
        var result = Validate(new TaskVersionBuilder());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TaskTypeKey.ShouldBe(TestEchoTaskType.TaskTypeKey);
        result.Value.ContentSchemaVersion.ShouldBe(1);
        result.Value.ContentHash.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Details_are_normalised()
    {
        // Arrange
        var builder = new TaskVersionBuilder()
            .WithTitle("  Fake title  ")
            .WithSummary(" Fake summary ")
            .WithTags(" CSharp ", "role:QA", "csharp", "C#")
            .WithInterviewerNotes("   ");

        // Act
        var details = Validate(builder).Value.Details;

        // Assert
        details.Title.ShouldBe("Fake title");
        details.Summary.ShouldBe("Fake summary");
        details.Tags.ShouldBe(["csharp", "role:qa", "c#"]);
        details.InterviewerNotes.ShouldBeNull();
    }

    [Fact]
    public void Missing_tags_are_no_tags()
    {
        // Act
        var details = Validate(new TaskVersionBuilder().WithTags(null)).Value.Details;

        // Assert
        details.Tags.ShouldBe([]);
    }

    [Fact]
    public void Limits_are_inclusive()
    {
        // Arrange
        var atUpperLimits = new TaskVersionBuilder()
            .WithTitle(new string('t', TaskVersionDetails.TitleMaxLength))
            .WithSummary(new string('s', TaskVersionDetails.SummaryMaxLength))
            .WithEstimatedMinutes(TaskVersionDetails.MaxEstimatedMinutes)
            .WithTags([.. Enumerable.Range(1, TaskTags.MaxCount - 1).Select(index => $"tag-{index}"), new string('x', TaskTags.MaxLength)])
            .WithRubric(_scale with { Weight = 10 }, _scale with { Id = "fake-other", Weight = 1 });
        var atLowerLimits = new TaskVersionBuilder().WithEstimatedMinutes(TaskVersionDetails.MinEstimatedMinutes);

        // Act
        var upper = Validate(atUpperLimits);
        var lower = Validate(atLowerLimits);

        // Assert
        upper.IsSuccess.ShouldBeTrue();
        lower.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Duplicate_tags_count_once()
    {
        // Arrange
        var tags = Enumerable.Range(1, TaskTags.MaxCount).Select(index => $"tag-{index}").Concat(["TAG-1", " tag-2 "]).ToArray();

        // Act
        var result = Validate(new TaskVersionBuilder().WithTags(tags));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Details.Tags!.Count.ShouldBe(TaskTags.MaxCount);
    }

    [Theory]
    [MemberData(nameof(InvalidDetails))]
    public void Invalid_details_fail_on_their_field(string example)
    {
        // Arrange
        var (build, field) = _invalid[example];

        // Act
        var result = Validate(build());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>().Errors.Keys.ShouldBe([field]);
    }

    [Fact]
    public void An_unregistered_task_type_fails()
    {
        // Act
        var result = TestTasks.Validator.Validate("retired-fake-type", new TaskVersionBuilder().Build());

        // Assert
        result.Error.ShouldBeOfType<ValidationError>().Errors.Keys.ShouldBe(["taskType"]);
    }

    [Fact]
    public void Generic_and_task_type_errors_are_reported_together()
    {
        // Arrange
        var builder = new TaskVersionBuilder().WithTitle(" ").WithLevel(null).WithPhrase(" ");

        // Act
        var result = Validate(builder);

        // Assert
        result.Error.ShouldBeOfType<ValidationError>().Errors.Keys.ShouldBe(["title", "level", "candidateContent.phrase"], ignoreOrder: true);
    }

    [Fact]
    public void The_content_hash_changes_with_any_detail_and_ignores_json_formatting()
    {
        // Arrange
        var original = new TaskVersionBuilder().WithTitle("Fake title");
        var reformatted = new TaskVersionBuilder()
            .WithTitle("Fake title")
            .WithCandidateContent(TaskVersionBuilder.Json("""
                {
                    "phrase":   "Hello, fake world"
                }
                """));
        var changed = new TaskVersionBuilder().WithTitle("Fake title").WithReferenceAnswer("Another made-up answer.");

        // Act
        var hash = Validate(original).Value.ContentHash;

        // Assert
        Validate(reformatted).Value.ContentHash.ShouldBe(hash);
        Validate(changed).Value.ContentHash.ShouldNotBe(hash);
    }

    private static Result<ValidatedTaskVersion> Validate(TaskVersionBuilder builder) =>
        TestTasks.Validator.Validate(TestEchoTaskType.TaskTypeKey, builder.Build());
}
