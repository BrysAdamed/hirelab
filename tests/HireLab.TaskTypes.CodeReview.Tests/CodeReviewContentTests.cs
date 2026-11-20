using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Catalog;
using Microsoft.Extensions.DependencyInjection;
using static HireLab.TaskTypes.CodeReview.Tests.CodeReviewBuilders;

namespace HireLab.TaskTypes.CodeReview.Tests;

public sealed class CodeReviewContentTests
{
    private static readonly ITaskType _codeReview = new CodeReviewTaskType();

    [Fact]
    public void The_catalog_ships_the_code_review_type()
    {
        // Act
        var registry = new ServiceCollection().AddTaskTypes().BuildServiceProvider().GetRequiredService<ITaskTypeRegistry>();

        // Assert
        var codeReview = registry.Find("code-review").ShouldBeOfType<CodeReviewTaskType>();
        codeReview.ContentSchemaVersion.ShouldBe(1);
        codeReview.ResponseSchemaVersion.ShouldBe(1);
    }

    [Fact]
    public void A_pull_request_with_modified_added_and_deleted_files_and_findings_on_both_sides_is_valid()
    {
        // Arrange
        var staff = new CodeReviewStaffContent(
        [
            Finding,
            Finding with { Id = "unused-coupon", File = DeletedPath, Side = DiffSide.Original, StartLine = 1, EndLine = 1, Severity = FindingSeverity.Nit },
            Finding with { Id = "discount-range", File = AddedPath, Side = DiffSide.Modified, StartLine = 1, EndLine = 1, Severity = FindingSeverity.Critical },
        ]);

        // Act
        var result = Validate(Candidate, staff);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_pull_request_without_expected_findings_and_with_an_empty_description_is_valid()
    {
        // Act
        var result = Validate(Candidate with { Description = "" }, new CodeReviewStaffContent([]));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Content_at_every_limit_is_valid()
    {
        // Arrange
        var side = new string('a', CodeReviewTaskType.SideMaxBytes);
        var files = Enumerable.Range(0, CodeReviewTaskType.MaxFiles)
            .Select(index => new ChangedFile($"src/fake-{index}.ts".PadRight(CodeReviewTaskType.PathMaxLength, 'x'), "typescript", "a", "b"))
            .ToList();
        files[0] = files[0] with { Original = side, Modified = side };
        var candidate = new CodeReviewCandidateContent(
            new string('t', CodeReviewTaskType.TitleMaxLength),
            new string('d', CodeReviewTaskType.DescriptionMaxLength),
            files);
        var findings = Enumerable.Range(0, CodeReviewTaskType.MaxExpectedFindings)
            .Select(index => Finding with { Id = $"fake-finding-{index}", File = files[1].Path, StartLine = 1, EndLine = 1 })
            .ToList();

        // Act
        var result = Validate(candidate, new CodeReviewStaffContent(findings));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("csharp")]
    [InlineData("typescript")]
    [InlineData("javascript")]
    [InlineData("html")]
    [InlineData("css")]
    [InlineData("json")]
    [InlineData("sql")]
    [InlineData("xml")]
    [InlineData("yaml")]
    [InlineData("markdown")]
    [InlineData("plaintext")]
    public void Every_listed_language_is_valid(string language)
    {
        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile with { Language = language }] }, new CodeReviewStaffContent([]));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("python")]
    [InlineData("CSharp")]
    [InlineData("")]
    public void A_language_off_the_list_is_refused(string language)
    {
        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile, AddedFile with { Language = language }] }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.files[1].language"].ShouldHaveSingleItem().ShouldStartWith("Choose one of the languages: csharp, typescript");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_title_is_refused(string title)
    {
        // Act
        var result = Validate(Candidate with { Title = title }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.title"].ShouldBe(["Enter a title."]);
    }

    [Fact]
    public void A_title_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(Candidate with { Title = new string('t', CodeReviewTaskType.TitleMaxLength + 1) }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.title"].ShouldBe(["Keep the title to at most 200 characters."]);
    }

    [Fact]
    public void A_description_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(Candidate with { Description = new string('d', CodeReviewTaskType.DescriptionMaxLength + 1) }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.description"].ShouldBe(["Keep the description to at most 20,000 characters."]);
    }

    [Fact]
    public void No_changed_files_are_refused()
    {
        // Act
        var result = Validate(Candidate with { Files = [] }, new CodeReviewStaffContent([]));

        // Assert
        ErrorsOf(result)["candidateContent.files"].ShouldBe(["Add 1 to 20 changed files."]);
    }

    [Fact]
    public void More_than_20_changed_files_are_refused()
    {
        // Arrange
        var files = Enumerable.Range(0, CodeReviewTaskType.MaxFiles + 1)
            .Select(index => AddedFile with { Path = $"src/fake-{index}.cs" })
            .ToList();

        // Act
        var result = Validate(Candidate with { Files = files }, new CodeReviewStaffContent([]));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent.files"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_path_is_refused(string path)
    {
        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile, AddedFile with { Path = path }] }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.files[1].path"].ShouldBe(["Enter a path."]);
    }

    [Fact]
    public void A_path_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(
            Candidate with { Files = [ModifiedFile, AddedFile with { Path = new string('p', CodeReviewTaskType.PathMaxLength + 1) }] },
            Staff);

        // Assert
        ErrorsOf(result)["candidateContent.files[1].path"].ShouldBe(["Keep the path to at most 300 characters."]);
    }

    [Fact]
    public void A_path_used_twice_is_refused()
    {
        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile, AddedFile, AddedFile] }, Staff);

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent.files[2].path"]);
        ErrorsOf(result)["candidateContent.files[2].path"].ShouldBe(["Another changed file already uses this path."]);
    }

    [Fact]
    public void A_file_without_either_side_is_refused()
    {
        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile, new ChangedFile(AddedPath, "csharp")] }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.files[1]"].ShouldBe(["Enter the original text, the modified text or both."]);
    }

    [Fact]
    public void A_side_over_200_KB_is_refused()
    {
        // Arrange
        var tooLarge = new string('ż', (CodeReviewTaskType.SideMaxBytes / 2) + 1);

        // Act
        var result = Validate(Candidate with { Files = [ModifiedFile with { Original = tooLarge, Modified = tooLarge }] }, Staff);

        // Assert
        ErrorsOf(result)["candidateContent.files[0].original"].ShouldBe(["Keep the text to at most 200 KB."]);
        ErrorsOf(result)["candidateContent.files[0].modified"].ShouldBe(["Keep the text to at most 200 KB."]);
    }

    [Fact]
    public void A_missing_changed_file_entry_is_refused()
    {
        // Act
        var result = _codeReview.ValidateContent(
            Json("""{ "title": "Fake", "description": "", "files": [null] }"""),
            Json("""{ "expectedFindings": [] }"""));

        // Assert
        ErrorsOf(result)["candidateContent.files[0]"].ShouldBe(["Define the changed file."]);
    }

    [Fact]
    public void More_than_50_expected_findings_are_refused()
    {
        // Arrange
        var findings = Enumerable.Range(0, CodeReviewTaskType.MaxExpectedFindings + 1)
            .Select(index => Finding with { Id = $"fake-finding-{index}" })
            .ToList();

        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent(findings));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["staffContent.expectedFindings"]);
        ErrorsOf(result)["staffContent.expectedFindings"].ShouldBe(["Add at most 50 expected findings."]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Missing-Null-Check")]
    [InlineData("missing null check")]
    [InlineData("-missing")]
    [InlineData("1-missing")]
    public void An_expected_finding_id_that_is_not_kebab_case_is_refused(string id)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Id = id }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].id"].ShouldBe(["Use a kebab-case id, for example 'missing-null-check'."]);
    }

    [Fact]
    public void An_expected_finding_id_used_twice_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding, Finding]));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["staffContent.expectedFindings[1].id"]);
        ErrorsOf(result)["staffContent.expectedFindings[1].id"].ShouldBe(["Another expected finding already uses this id."]);
    }

    [Theory]
    [InlineData("src/FakeShop/Unknown.cs")]
    [InlineData("SRC/FAKESHOP/FAKEORDERHANDLER.CS")]
    [InlineData("")]
    public void An_expected_finding_on_a_file_not_in_the_pull_request_is_refused(string file)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { File = file }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].file"].ShouldBe(["Choose the path of one of the changed files."]);
    }

    [Fact]
    public void An_expected_finding_on_the_original_side_of_an_added_file_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { File = AddedPath, Side = DiffSide.Original, StartLine = 1, EndLine = 1 }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].side"].ShouldBe(["The file has no original text; it is an added file."]);
    }

    [Fact]
    public void An_expected_finding_on_the_modified_side_of_a_deleted_file_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { File = DeletedPath, Side = DiffSide.Modified, StartLine = 1, EndLine = 1 }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].side"].ShouldBe(["The file has no modified text; it is a deleted file."]);
    }

    [Fact]
    public void An_expected_finding_on_an_undefined_side_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Side = (DiffSide)7 }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].side"].ShouldBe(["Choose the original or the modified side."]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 2)]
    public void An_expected_finding_starting_before_line_1_is_refused(int startLine, int endLine)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { StartLine = startLine, EndLine = endLine }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].startLine"].ShouldBe(["Start at line 1 or later."]);
    }

    [Fact]
    public void An_expected_finding_ending_before_it_starts_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { StartLine = 4, EndLine = 3 }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].endLine"].ShouldBe(["End at the start line or later."]);
    }

    [Theory]
    [InlineData(DiffSide.Original, 5)]
    [InlineData(DiffSide.Modified, 8)]
    public void An_expected_finding_may_end_on_the_empty_line_after_a_trailing_line_break(DiffSide side, int lastLine)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Side = side, StartLine = 1, EndLine = lastLine }]));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DiffSide.Original, 6, "The original side has 5 lines.")]
    [InlineData(DiffSide.Modified, 9, "The modified side has 8 lines.")]
    public void An_expected_finding_ending_after_the_last_line_of_its_side_is_refused(DiffSide side, int endLine, string message)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Side = side, StartLine = 2, EndLine = endLine }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].endLine"].ShouldBe([message]);
    }

    [Theory]
    [InlineData("one\r\ntwo\r\nthree", 3)]
    [InlineData("one\rtwo\rthree", 3)]
    [InlineData("", 1)]
    public void Lines_are_counted_across_every_line_break_style(string text, int lineCount)
    {
        // Arrange
        var candidate = Candidate with { Files = [AddedFile with { Modified = text }] };
        var finding = Finding with { File = AddedPath, StartLine = 1 };

        // Act
        var atLast = Validate(candidate, new CodeReviewStaffContent([finding with { EndLine = lineCount }]));
        var afterLast = Validate(candidate, new CodeReviewStaffContent([finding with { EndLine = lineCount + 1 }]));

        // Assert
        atLast.IsSuccess.ShouldBeTrue();
        ErrorsOf(afterLast).Keys.ShouldBe(["staffContent.expectedFindings[0].endLine"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void An_expected_finding_without_a_title_is_refused(string title)
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Title = title }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].title"].ShouldBe(["Enter a title."]);
    }

    [Fact]
    public void An_expected_finding_title_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Title = new string('t', CodeReviewTaskType.TitleMaxLength + 1) }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].title"].ShouldBe(["Keep the title to at most 200 characters."]);
    }

    [Fact]
    public void An_expected_finding_description_over_the_limit_is_refused()
    {
        // Act
        var result = Validate(
            Candidate,
            new CodeReviewStaffContent([Finding with { Description = new string('d', CodeReviewTaskType.DescriptionMaxLength + 1) }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].description"].ShouldBe(["Keep the description to at most 20,000 characters."]);
    }

    [Fact]
    public void An_undefined_severity_is_refused()
    {
        // Act
        var result = Validate(Candidate, new CodeReviewStaffContent([Finding with { Severity = (FindingSeverity)9 }]));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0].severity"].ShouldBe(["Choose Critical, Major, Minor or Nit."]);
    }

    [Fact]
    public void A_missing_expected_finding_entry_is_refused()
    {
        // Act
        var result = _codeReview.ValidateContent(ToJson(Candidate), Json("""{ "expectedFindings": [null] }"""));

        // Assert
        ErrorsOf(result)["staffContent.expectedFindings[0]"].ShouldBe(["Define the expected finding."]);
    }

    [Fact]
    public void Every_broken_rule_is_reported_at_once()
    {
        // Act
        var result = Validate(
            Candidate with { Title = "", Files = [ModifiedFile with { Language = "cobol" }] },
            new CodeReviewStaffContent([Finding with { Id = "Bad Id", Severity = (FindingSeverity)9 }]));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(
            ["candidateContent.title", "candidateContent.files[0].language", "staffContent.expectedFindings[0].id", "staffContent.expectedFindings[0].severity"],
            ignoreOrder: true);
    }

    [Theory]
    [InlineData("""{ "title": "Fake", "description": "" }""")]
    [InlineData("""{ "title": "Fake", "description": "", "files": [], "labels": [] }""")]
    [InlineData("""{ "title": null, "description": "", "files": [] }""")]
    [InlineData("""{ "title": "Fake", "description": "", "files": [{ "path": "fake.cs", "language": "csharp", "original": 42 }] }""")]
    public void Candidate_content_not_matching_the_model_is_refused(string candidateContent)
    {
        // Act
        var result = _codeReview.ValidateContent(Json(candidateContent), ToJson(Staff));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["candidateContent"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("""{ "expectedFindings": [{ "id": "fake", "file": "src/FakeShop/FakeOrderHandler.cs", "side": "left", "startLine": 1, "endLine": 1, "title": "Fake", "description": "", "severity": "Major" }] }""")]
    [InlineData("""{ "expectedFindings": [{ "id": "fake", "file": "src/FakeShop/FakeOrderHandler.cs", "side": "modified", "startLine": 1, "endLine": 1, "title": "Fake", "description": "", "severity": "Blocker" }] }""")]
    [InlineData("""{ "expectedFindings": [{ "id": "fake", "file": "src/FakeShop/FakeOrderHandler.cs", "side": "modified", "startLine": 1, "endLine": 1, "title": "Fake", "description": "" }] }""")]
    public void Staff_content_not_matching_the_model_is_refused(string staffContent)
    {
        // Act
        var result = _codeReview.ValidateContent(ToJson(Candidate), Json(staffContent));

        // Assert
        ErrorsOf(result).Keys.ShouldBe(["staffContent"]);
    }

    [Fact]
    public void Content_reads_the_documented_wire_values()
    {
        // Arrange
        const string candidate = """
            { "title": "Fake", "description": "",
              "files": [{ "path": "fake.cs", "language": "csharp", "original": "a", "modified": "b" }, { "path": "new.cs", "language": "csharp", "modified": "c" }] }
            """;
        const string staff = """
            { "expectedFindings": [{ "id": "fake", "file": "fake.cs", "side": "original", "startLine": 1, "endLine": 1, "title": "Fake", "description": "", "severity": "Nit" }] }
            """;

        // Act
        var result = _codeReview.ValidateContent(Json(candidate), Json(staff));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ToJson(Finding).GetProperty("side").GetString().ShouldBe("modified");
        ToJson(Finding).GetProperty("severity").GetString().ShouldBe("Major");
    }

    private static Result Validate(CodeReviewCandidateContent candidate, CodeReviewStaffContent staff) =>
        _codeReview.ValidateContent(ToJson(candidate), ToJson(staff));
}
