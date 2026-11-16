using HireLab.Modules.TaskBank.Seeds;
using HireLab.Modules.TaskBank.Tests.Tasks;
using HireLab.Testing.TaskTypes;

namespace HireLab.Modules.TaskBank.Tests.Seeds;

public sealed class SeedPackageValidatorTests
{
    private readonly SeedPackageValidator _validator = new(TestTasks.TaskTypes, TestTasks.Validator);

    [Fact]
    public void A_valid_package_is_validated_for_its_task_type()
    {
        // Arrange
        var package = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")).Read().Single();

        // Act
        var validated = _validator.Validate(package);

        // Assert
        validated.TaskTypeKey.ShouldBe(TestEchoTaskType.TaskTypeKey);
        validated.ContentHash.ShouldBe(TestTasks.Validator.Validate(package.TaskTypeKey, package.Details).Value.ContentHash);
    }

    [Fact]
    public void The_content_hash_ignores_how_the_package_is_written()
    {
        // Arrange
        var fromFile = new TestEchoSeedPackage("fake-echo-task");
        var fromCrlfFile = new TestEchoSeedPackage("fake-echo-task")
            .WithFile(TestEchoSeedPackage.ReferenceAnswerFile, TestEchoSeedPackage.ReferenceAnswer.Replace("\n", "\r\n", StringComparison.Ordinal));
        var inline = new TestEchoSeedPackage("fake-echo-task")
            .With("referenceAnswer", TestEchoSeedPackage.ReferenceAnswer)
            .WithoutFile(TestEchoSeedPackage.ReferenceAnswerFile);

        // Act
        var hashes = new[] { fromFile, fromCrlfFile, inline }
            .Select(package => _validator.Validate(new FakeSeedPackages(package).Read().Single()).ContentHash)
            .ToList();

        // Assert
        hashes.Distinct().ShouldHaveSingleItem();
    }

    [Fact]
    public void The_content_hash_changes_with_the_content()
    {
        // Arrange
        var original = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")).Read().Single();
        var changed = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").WithPhrase("A changed fake phrase")).Read().Single();

        // Act
        var originalHash = _validator.Validate(original).ContentHash;
        var changedHash = _validator.Validate(changed).ContentHash;

        // Assert
        changedHash.ShouldNotBe(originalHash);
    }

    [Fact]
    public void An_unknown_task_type_is_rejected()
    {
        // Arrange
        var package = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task").With("taskType", "fake-unknown-type")).Read().Single();

        // Act
        var exception = Should.Throw<SeedPackageException>(() => _validator.Validate(package));

        // Assert
        exception.Message.ShouldBe("Seed package fake-echo-task/v1 uses the task type 'fake-unknown-type', which isn't registered.");
    }

    [Fact]
    public void Invalid_details_and_content_are_listed_by_field()
    {
        // Arrange
        var package = new FakeSeedPackages(new TestEchoSeedPackage("fake-echo-task")
            .WithPhrase(" ")
            .With("estimatedMinutes", 0))
            .Read()
            .Single();

        // Act
        var exception = Should.Throw<SeedPackageException>(() => _validator.Validate(package));

        // Assert
        exception.Message.ShouldStartWith("Seed package fake-echo-task/v1 is invalid: ");
        exception.Message.ShouldContain("estimatedMinutes: ");
        exception.Message.ShouldContain("candidateContent.phrase: Enter a phrase.");
    }
}
