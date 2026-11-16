using Microsoft.Extensions.DependencyInjection;

namespace HireLab.TaskTypes.Abstractions.Tests;

public sealed class TaskTypeRegistryTests
{
    [Fact]
    public void Registry_holds_every_registered_task_type_ordered_by_key()
    {
        // Arrange
        var services = new ServiceCollection().AddTaskType<QuizTaskType>().AddTaskType<PollTaskType>();

        // Act
        var registry = Resolve(services);

        // Assert
        registry.All.Select(taskType => taskType.Key).ShouldBe(["poll", "quiz"]);
    }

    [Fact]
    public void Registry_finds_a_task_type_by_key()
    {
        // Arrange
        var registry = Resolve(new ServiceCollection().AddTaskType<QuizTaskType>());

        // Act
        var taskType = registry.Find(QuizTaskType.QuizKey);

        // Assert
        taskType.ShouldBeOfType<QuizTaskType>();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("Quiz")]
    [InlineData("")]
    public void Registry_finds_nothing_for_an_unknown_key(string key)
    {
        // Arrange
        var registry = Resolve(new ServiceCollection().AddTaskType<QuizTaskType>());

        // Act
        var taskType = registry.Find(key);

        // Assert
        taskType.ShouldBeNull();
    }

    [Fact]
    public void Registry_is_empty_without_task_types()
    {
        // Act
        var registry = Resolve(new ServiceCollection().AddTaskTypeRegistry());

        // Assert
        registry.All.ShouldBeEmpty();
    }

    [Fact]
    public void Registering_the_same_task_type_twice_registers_it_once()
    {
        // Arrange
        var services = new ServiceCollection().AddTaskType<QuizTaskType>().AddTaskType<QuizTaskType>();

        // Act
        var registry = Resolve(services);

        // Assert
        registry.All.Count.ShouldBe(1);
    }

    [Fact]
    public void Two_task_types_with_the_same_key_fail()
    {
        // Arrange
        var services = new ServiceCollection().AddTaskType<QuizTaskType>().AddTaskType<SecondQuizTaskType>();

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(services));

        // Assert
        exception.Message.ShouldContain("'quiz'");
        exception.Message.ShouldContain(typeof(SecondQuizTaskType).FullName!);
    }

    [Fact]
    public void A_key_that_is_not_kebab_case_fails()
    {
        // Arrange
        var services = new ServiceCollection().AddTaskType<BadlyKeyedTaskType>();

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(services));

        // Assert
        exception.Message.ShouldContain("kebab-case");
    }

    [Fact]
    public void A_schema_version_below_one_fails()
    {
        // Arrange
        var services = new ServiceCollection().AddTaskType<UnversionedTaskType>();

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => Resolve(services));

        // Assert
        exception.Message.ShouldContain("schema versions");
    }

    private static ITaskTypeRegistry Resolve(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ITaskTypeRegistry>();
    }
}
