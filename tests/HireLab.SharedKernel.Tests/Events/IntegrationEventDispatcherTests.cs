using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HireLab.SharedKernel.Tests.Events;

public sealed class IntegrationEventDispatcherTests
{
    [Fact]
    public async Task Every_handler_runs_and_a_failing_handler_is_logged_without_stopping_the_others()
    {
        // Arrange
        var failing = Substitute.For<IIntegrationEventHandler<NoteCreated>>();
        failing.HandleAsync(Arg.Any<NoteCreated>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Reaction failed."));
        var succeeding = Substitute.For<IIntegrationEventHandler<NoteCreated>>();

        var services = new ServiceCollection();
        services.AddSingleton(failing);
        services.AddSingleton(succeeding);
        await using var provider = services.BuildServiceProvider();

        var logger = new FakeLogger<IntegrationEventDispatcher>();
        var dispatcher = new IntegrationEventDispatcher(provider, logger);
        var integrationEvent = new NoteCreated(Guid.CreateVersion7());

        // Act
        await dispatcher.DispatchAsync([integrationEvent], TestContext.Current.CancellationToken);

        // Assert
        await succeeding.Received(1).HandleAsync(integrationEvent, Arg.Any<CancellationToken>());
        logger.LatestRecord.Level.ShouldBe(LogLevel.Error);
        logger.LatestRecord.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Event_without_handlers_is_ignored()
    {
        // Arrange
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var logger = new FakeLogger<IntegrationEventDispatcher>();
        var dispatcher = new IntegrationEventDispatcher(provider, logger);

        // Act
        await dispatcher.DispatchAsync([new NoteCreated(Guid.CreateVersion7())], TestContext.Current.CancellationToken);

        // Assert
        logger.Collector.Count.ShouldBe(0);
    }
}
