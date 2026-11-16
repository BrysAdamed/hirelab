using System.Text;
using HireLab.Modules.TaskBank.Seeds;

namespace HireLab.Tasks.Tests;

public sealed class SeedPackageHashTests
{
    [Fact]
    public void The_hash_is_lowercase_sha256_hex()
    {
        // Act
        var hash = SeedPackageHash.Compute([File("task.json", "{}")]);

        // Assert
        hash.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Line_endings_and_file_order_do_not_change_the_hash()
    {
        // Arrange
        var lf = new[] { File("task.json", "{}\n"), File("notes.md", "First fake line\nSecond fake line\n") };
        var crlf = new[] { File("notes.md", "First fake line\r\nSecond fake line\r\n"), File("task.json", "{}\r\n") };

        // Act & Assert
        SeedPackageHash.Compute(crlf).ShouldBe(SeedPackageHash.Compute(lf));
    }

    [Theory]
    [InlineData("notes.md", "Fake notes, edited\n")]
    [InlineData("renamed.md", "Fake notes\n")]
    [InlineData("notes.md", "Fake notes \n")]
    public void Any_change_of_a_file_or_its_path_changes_the_hash(string path, string text)
    {
        // Arrange
        var original = new[] { File("task.json", "{}"), File("notes.md", "Fake notes\n") };
        var changed = new[] { File("task.json", "{}"), File(path, text) };

        // Act & Assert
        SeedPackageHash.Compute(changed).ShouldNotBe(SeedPackageHash.Compute(original));
    }

    [Fact]
    public void Content_cannot_move_between_a_path_and_the_next_file()
    {
        // Arrange
        var first = new[] { File("a", "bc"), File("d", "") };
        var second = new[] { File("a", "b"), File("cd", "") };

        // Act & Assert
        SeedPackageHash.Compute(first).ShouldNotBe(SeedPackageHash.Compute(second));
    }

    [Fact]
    public void A_lone_carriage_return_is_kept()
    {
        // Act & Assert
        SeedPackageHash.Compute([File("notes.md", "a\rb")]).ShouldNotBe(SeedPackageHash.Compute([File("notes.md", "a\nb")]));
    }

    private static SeedPackageFile File(string path, string text) => new(path, Encoding.UTF8.GetBytes(text));
}
