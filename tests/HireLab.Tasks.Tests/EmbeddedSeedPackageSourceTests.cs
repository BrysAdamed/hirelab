using HireLab.Modules.TaskBank.Seeds;

namespace HireLab.Tasks.Tests;

public sealed class EmbeddedSeedPackageSourceTests
{
    private readonly EmbeddedSeedPackageSource _fixtures = new(typeof(EmbeddedSeedPackageSourceTests).Assembly, "fixture/");

    [Fact]
    public void Embedded_files_keep_their_path_below_the_tasks_folder()
    {
        // Act
        var files = _fixtures.ReadFiles();

        // Assert
        files.Select(file => file.Path).ShouldBe(
            ["fake-embedded-task/v1/content/phrase.txt", "fake-embedded-task/v1/reference-answer.md", "fake-embedded-task/v1/task.json"],
            ignoreOrder: true);
    }

    [Fact]
    public void Embedded_packages_are_read_with_their_files_inlined()
    {
        // Act
        var package = SeedPackageReader.Read(_fixtures.ReadFiles()).ShouldHaveSingleItem();

        // Assert
        package.Name.ShouldBe("fake-embedded-task/v1");
        package.Details.ReferenceAnswer.ShouldBe("A made-up embedded reference answer.\n");
        package.Details.CandidateContent.GetProperty("phrase").GetString().ShouldBe("Hello, embedded fake world");
    }

    [Fact]
    public void Only_resources_with_the_prefix_are_read()
    {
        // Arrange
        var source = new EmbeddedSeedPackageSource(typeof(EmbeddedSeedPackageSourceTests).Assembly, "fake-other-prefix/");

        // Act & Assert
        source.ReadFiles().ShouldBeEmpty();
    }
}
