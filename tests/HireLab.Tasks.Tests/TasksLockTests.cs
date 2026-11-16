using HireLab.Modules.TaskBank.Seeds;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Tasks.Tests;

public sealed class TasksLockTests
{
    [Fact]
    public void Seed_packages_are_valid_and_match_the_tasks_lock()
    {
        // Arrange
        var packages = new EmbeddedSeedPackageSource(SeedTaskPackages.Assembly, SeedTaskPackages.ResourcePrefix);
        var taskTypes = new ServiceCollection().AddTaskTypes().BuildServiceProvider().GetRequiredService<ITaskTypeRegistry>();

        // Act
        var difference = TasksLockGuard.Check(packages, taskTypes, Repository.TasksLockPath, TasksLockGuard.UpdateRequested);

        // Assert
        difference.IsEmpty.ShouldBeTrue(difference.ToString());
    }
}
