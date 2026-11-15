using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Handlers.Decorators;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Tests.TestDoubles;

namespace HireLab.SharedKernel.Tests.Handlers;

public sealed class UnitOfWorkDecoratorTests
{
    private readonly ICommandHandler<CreateNote, Result<Guid>> _inner = Substitute.For<ICommandHandler<CreateNote, Result<Guid>>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IntegrationEventQueue _eventQueue = new();
    private readonly IIntegrationEventDispatcher _eventDispatcher = Substitute.For<IIntegrationEventDispatcher>();
    private readonly UnitOfWorkDecorator<CreateNote, Result<Guid>> _decorator;

    public UnitOfWorkDecoratorTests()
    {
        _decorator = new UnitOfWorkDecorator<CreateNote, Result<Guid>>(_inner, _unitOfWork, _eventQueue, _eventDispatcher);
    }

    [Fact]
    public async Task Success_saves_changes_then_dispatches_the_raised_events()
    {
        // Arrange
        var noteId = Guid.CreateVersion7();
        var raised = new NoteCreated(noteId);
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _eventQueue.Enqueue(raised);
                return Result.Success(noteId);
            });

        // Act
        var result = await _decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(noteId);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _eventDispatcher.DispatchAsync(
                Arg.Is<IReadOnlyList<IIntegrationEvent>>(events => Equals(events.Single(), raised)),
                Arg.Any<CancellationToken>());
        });
        _eventQueue.DequeueAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task Success_without_events_saves_and_dispatches_nothing()
    {
        // Arrange
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.CreateVersion7()));

        // Act
        await _decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IIntegrationEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failure_neither_saves_nor_dispatches_and_discards_the_raised_events()
    {
        // Arrange
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _eventQueue.Enqueue(new NoteCreated(Guid.CreateVersion7()));
                return (Result<Guid>)NoteErrors.NotFound;
            });

        // Act
        var result = await _decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        result.Error.ShouldBe(NoteErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _eventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IIntegrationEvent>>(), Arg.Any<CancellationToken>());
        _eventQueue.DequeueAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task Failed_save_propagates_and_dispatches_nothing()
    {
        // Arrange
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _eventQueue.Enqueue(new NoteCreated(Guid.CreateVersion7()));
                return Result.Success(Guid.CreateVersion7());
            });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<int>(_ => throw new InvalidOperationException("Database unavailable."));

        // Act
        var act = () => _decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        await _eventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IIntegrationEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Events_are_dispatched_even_if_the_request_is_cancelled_after_the_commit()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _eventQueue.Enqueue(new NoteCreated(Guid.CreateVersion7()));
                return Result.Success(Guid.CreateVersion7());
            });

        // Act
        await _decorator.HandleAsync(new CreateNote("Ask about caching"), cancellation.Token);

        // Assert
        await _eventDispatcher.Received(1).DispatchAsync(Arg.Any<IReadOnlyList<IIntegrationEvent>>(), CancellationToken.None);
    }
}
