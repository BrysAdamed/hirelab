using HireLab.Modules.TaskBank.Seeds;
using HireLab.TaskTypes.Abstractions;
using HireLab.Testing.TaskTypes;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Tasks.Tests;

public sealed class TasksLockGuardTests : IDisposable
{
    private static readonly ITaskTypeRegistry _taskTypes = new ServiceCollection()
        .AddTaskType<TestEchoTaskType>()
        .BuildServiceProvider()
        .GetRequiredService<ITaskTypeRegistry>();

    private readonly string _directory = Directory.CreateTempSubdirectory("hirelab-tasks-lock-").FullName;

    private string LockPath => Path.Combine(_directory, TasksLock.FileName);

    [Fact]
    public void Packages_that_match_the_lock_pass()
    {
        // Arrange
        var packages = new FixturePackages(new TestEchoSeedPackage("fake-echo-task", 1), new TestEchoSeedPackage("fake-echo-task", 2));
        Lock(packages);

        // Act
        var difference = Check(packages);

        // Assert
        difference.IsEmpty.ShouldBeTrue(difference.ToString());
    }

    [Fact]
    public void A_changed_locked_version_fails()
    {
        // Arrange
        Lock(new FixturePackages(new TestEchoSeedPackage("fake-echo-task")));
        var changed = new FixturePackages(new TestEchoSeedPackage("fake-echo-task").WithFile(TestEchoSeedPackage.ReferenceAnswerFile, "An edited fake answer."));

        // Act
        var difference = Check(changed);

        // Assert
        difference.Changed.ShouldBe(["fake-echo-task/v1"]);
        difference.ToString().ShouldContain("Published seed versions are immutable");
    }

    [Fact]
    public void A_removed_locked_version_fails()
    {
        // Arrange
        Lock(new FixturePackages(new TestEchoSeedPackage("fake-echo-task", 1), new TestEchoSeedPackage("fake-echo-task", 2)));

        // Act
        var difference = Check(new FixturePackages(new TestEchoSeedPackage("fake-echo-task", 1)));

        // Assert
        difference.Removed.ShouldBe(["fake-echo-task/v2"]);
    }

    [Fact]
    public void A_version_missing_from_the_lock_fails()
    {
        // Arrange
        Lock(new FixturePackages(new TestEchoSeedPackage("fake-echo-task")));
        var packages = new FixturePackages(new TestEchoSeedPackage("fake-echo-task"), new TestEchoSeedPackage("fake-echo-other"));

        // Act
        var difference = Check(packages);

        // Assert
        difference.Unlocked.ShouldBe(["fake-echo-other/v1"]);
        difference.ToString().ShouldContain("eng/update-tasks-lock.ps1");
    }

    [Fact]
    public void A_missing_lock_file_leaves_every_version_unlocked()
    {
        // Act
        var difference = Check(new FixturePackages(new TestEchoSeedPackage("fake-echo-task")));

        // Assert
        difference.Unlocked.ShouldBe(["fake-echo-task/v1"]);
    }

    [Fact]
    public void An_update_adds_new_versions_and_keeps_locked_hashes()
    {
        // Arrange
        Lock(new FixturePackages(new TestEchoSeedPackage("fake-echo-task")));
        var lockedHash = TasksLock.Load(LockPath).Versions["fake-echo-task/v1"];
        var packages = new FixturePackages(
            new TestEchoSeedPackage("fake-echo-task").WithPhrase("An edited fake phrase"),
            new TestEchoSeedPackage("fake-echo-task", 2));

        // Act
        var difference = TasksLockGuard.Check(packages, _taskTypes, LockPath, update: true);

        // Assert
        difference.Changed.ShouldBe(["fake-echo-task/v1"]);
        difference.Unlocked.ShouldBeEmpty();
        var updated = TasksLock.Load(LockPath);
        updated.Versions.Keys.ShouldBe(["fake-echo-task/v1", "fake-echo-task/v2"]);
        updated.Versions["fake-echo-task/v1"].ShouldBe(lockedHash);
    }

    [Fact]
    public void The_lock_file_is_sorted_indented_json_with_lf_line_endings()
    {
        // Arrange
        var packages = new FixturePackages(new TestEchoSeedPackage("fake-zulu"), new TestEchoSeedPackage("fake-alpha"));

        // Act
        Lock(packages);

        // Assert
        var text = File.ReadAllText(LockPath);
        text.ShouldNotContain("\r");
        text.ShouldEndWith("}\n");
        text.IndexOf("fake-alpha/v1", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("fake-zulu/v1", StringComparison.Ordinal));
    }

    [Fact]
    public void An_invalid_package_fails_the_guard()
    {
        // Arrange
        var packages = new FixturePackages(new TestEchoSeedPackage("fake-echo-task").WithPhrase(" "));

        // Act
        var exception = Should.Throw<SeedPackageException>(() => Check(packages));

        // Assert
        exception.Message.ShouldContain("candidateContent.phrase");
    }

    [Fact]
    public void A_package_of_an_unregistered_type_fails_the_guard()
    {
        // Arrange
        var packages = new FixturePackages(new TestEchoSeedPackage("fake-echo-task"));
        var noTaskTypes = new ServiceCollection().AddTaskTypeRegistry().BuildServiceProvider().GetRequiredService<ITaskTypeRegistry>();

        // Act
        var exception = Should.Throw<SeedPackageException>(() => TasksLockGuard.Check(packages, noTaskTypes, LockPath, update: false));

        // Assert
        exception.Message.ShouldContain("which isn't registered");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private void Lock(FixturePackages packages) =>
        TasksLockGuard.Check(packages, _taskTypes, LockPath, update: true).IsEmpty.ShouldBeTrue();

    private TasksLockDifference Check(FixturePackages packages) => TasksLockGuard.Check(packages, _taskTypes, LockPath, update: false);
}
