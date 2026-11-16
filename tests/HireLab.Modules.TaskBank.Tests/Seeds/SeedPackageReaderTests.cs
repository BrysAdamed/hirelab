using System.Text.Json.Nodes;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Seeds;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.TaskBank.Tests.Seeds;

public sealed class SeedPackageReaderTests
{
    [Fact]
    public void A_package_is_read_into_task_version_details()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithPhrase("Fake phrase"));

        // Act
        var package = packages.Read().ShouldHaveSingleItem();

        // Assert
        package.Slug.ShouldBe("fake-echo-task");
        package.Number.ShouldBe(1);
        package.Name.ShouldBe("fake-echo-task/v1");
        package.TaskTypeKey.ShouldBe(TestEchoTaskType.TaskTypeKey);
        package.Details.Title.ShouldBe("Fake Echo Task fake-echo-task");
        package.Details.Level.ShouldBe(TaskLevel.Mid);
        package.Details.EstimatedMinutes.ShouldBe(15);
        package.Details.Tags.ShouldBe(["fake", "testing"]);
        package.Details.ReferenceAnswer.ShouldBe(TestEchoSeedPackage.ReferenceAnswer);
        package.Details.InterviewerNotes.ShouldBeNull();
        package.Details.Rubric!.Criteria.Select(criterion => criterion.Id).ShouldBe(["fake-quality", "fake-echo"]);
        package.Details.CandidateContent.GetProperty("phrase").GetString().ShouldBe("Fake phrase");
        package.Files.Select(file => file.Path).ShouldBe(["task.json", TestEchoSeedPackage.ReferenceAnswerFile], ignoreOrder: true);
    }

    [Fact]
    public void A_file_reference_is_inlined_anywhere_in_task_json()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")
            .With("candidateContent", new JsonObject { ["phrase"] = TestEchoSeedPackage.FileReference("content/phrase.txt") })
            .With("interviewerNotes", TestEchoSeedPackage.FileReference("notes.md"))
            .WithFile("content/phrase.txt", "Fake phrase from a file")
            .WithFile("notes.md", "Made-up notes."));

        // Act
        var package = packages.Read().ShouldHaveSingleItem();

        // Assert
        package.Details.CandidateContent.GetProperty("phrase").GetString().ShouldBe("Fake phrase from a file");
        package.Details.InterviewerNotes.ShouldBe("Made-up notes.");
    }

    [Fact]
    public void An_inlined_file_loses_its_byte_order_mark_and_carriage_returns()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")
            .WithFile(TestEchoSeedPackage.ReferenceAnswerFile, "﻿First fake line\r\nSecond fake line\r\n"));

        // Act
        var package = packages.Read().ShouldHaveSingleItem();

        // Assert
        package.Details.ReferenceAnswer.ShouldBe("First fake line\nSecond fake line\n");
    }

    [Fact]
    public void Packages_are_ordered_by_slug_and_version()
    {
        // Arrange
        var packages = new FakeSeedPackages(
            new TestEchoSeedPackage("fake-zulu", 2),
            new TestEchoSeedPackage("fake-alpha"),
            new TestEchoSeedPackage("fake-zulu", 1));

        // Act
        var read = packages.Read();

        // Assert
        read.Select(package => package.Name).ShouldBe(["fake-alpha/v1", "fake-zulu/v1", "fake-zulu/v2"]);
    }

    [Fact]
    public void A_draft_field_is_rejected()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").With("draft", false));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain("fake-echo-task/v1");
        exception.Message.ShouldContain("'draft'");
    }

    [Theory]
    [InlineData(new[] { 1, 3 })]
    [InlineData(new[] { 2 })]
    public void Versions_must_run_from_v1_without_gaps(int[] numbers)
    {
        // Arrange
        var packages = new FakeSeedPackages([.. numbers.Select(number => new TestEchoSeedPackage("fake-echo-task", number))]);

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain("Seed task fake-echo-task has the versions");
    }

    [Theory]
    [InlineData("fake-echo-task/task.json")]
    [InlineData("Fake_Task/v1/task.json")]
    [InlineData("fake-echo-task/v0/task.json")]
    [InlineData("fake-echo-task/v01/task.json")]
    [InlineData("fake-echo-task/version1/task.json")]
    public void A_file_outside_a_package_folder_is_rejected(string path)
    {
        // Arrange
        var packages = new FakeSeedPackages().AddFile(path, "{}");

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain($"'{path}' is not inside a seed package folder");
    }

    [Fact]
    public void A_package_without_task_json_is_rejected()
    {
        // Arrange
        var packages = new FakeSeedPackages().AddFile("fake-echo-task/v1/notes.md", "Made-up notes.");

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldBe("Seed package fake-echo-task/v1 has no task.json.");
    }

    [Fact]
    public void A_reference_to_a_missing_file_is_rejected()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithoutFile(TestEchoSeedPackage.ReferenceAnswerFile));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain($"references the missing file '{TestEchoSeedPackage.ReferenceAnswerFile}'");
    }

    [Fact]
    public void A_file_task_json_does_not_reference_is_rejected()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithFile("leftover.md", "Made-up leftover."));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain("doesn't reference: leftover.md");
    }

    [Theory]
    [InlineData("../fake-other/v1/notes.md")]
    [InlineData("./notes.md")]
    [InlineData("content//notes.md")]
    [InlineData("/notes.md")]
    [InlineData("content\\notes.md")]
    [InlineData("task.json")]
    public void A_reference_must_be_a_plain_relative_path(string path)
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")
            .With("interviewerNotes", TestEchoSeedPackage.FileReference(path)));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain($"references '{path}'");
    }

    [Fact]
    public void A_reference_with_other_fields_is_rejected()
    {
        // Arrange
        var reference = TestEchoSeedPackage.FileReference(TestEchoSeedPackage.ReferenceAnswerFile);
        reference["format"] = "markdown";
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").With("referenceAnswer", reference));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain("has a file reference that isn't");
    }

    [Fact]
    public void A_referenced_file_must_be_utf8_text()
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithoutFile(TestEchoSeedPackage.ReferenceAnswerFile))
            .AddFile($"fake-echo-task/v1/{TestEchoSeedPackage.ReferenceAnswerFile}", [0xC3, 0x28]);

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldContain("which isn't UTF-8 text");
    }

    [Fact]
    public void Task_json_must_be_valid_json()
    {
        // Arrange
        var packages = new FakeSeedPackages().AddFile("fake-echo-task/v1/task.json", "{ \"title\": ");

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldStartWith("fake-echo-task/v1/task.json is not valid JSON");
    }

    [Theory]
    [InlineData("titel")]
    [InlineData("content")]
    public void An_unknown_field_is_rejected(string field)
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").With(field, "Fake value"));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldStartWith("fake-echo-task/v1/task.json doesn't match the seed package format");
    }

    [Theory]
    [InlineData("taskType")]
    [InlineData("rubric")]
    [InlineData("candidateContent")]
    public void A_missing_required_field_is_rejected(string field)
    {
        // Arrange
        var packages = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").Without(field));

        // Act
        var exception = Should.Throw<SeedPackageException>(packages.Read);

        // Assert
        exception.Message.ShouldStartWith("fake-echo-task/v1/task.json doesn't match the seed package format");
    }
}
