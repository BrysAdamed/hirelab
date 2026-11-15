using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Handlers.Decorators;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HireLab.SharedKernel.Tests.Handlers;

public sealed class LoggingDecoratorTests
{
    private readonly FakeLogger _logger = new();

    [Fact]
    public async Task Success_is_logged_with_the_request_name_only()
    {
        // Arrange
        var inner = Substitute.For<ICommandHandler<CreateNote, Result<Guid>>>();
        var noteId = Guid.CreateVersion7();
        inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>()).Returns(Result.Success(noteId));
        var decorator = new LoggingDecorator.Command<CreateNote, Result<Guid>>(inner, _logger);

        // Act
        var result = await decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(noteId);
        var record = _logger.LatestRecord;
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldStartWith($"Handled {nameof(CreateNote)} in");
        _logger.Collector.GetSnapshot().ShouldAllBe(entry => !entry.Message.Contains("Ask about caching"));
    }

    [Fact]
    public async Task Failure_is_logged_with_the_error_code_and_type()
    {
        // Arrange
        var inner = Substitute.For<IQueryHandler<GetNote, Result<string>>>();
        inner.HandleAsync(Arg.Any<GetNote>(), Arg.Any<CancellationToken>()).Returns((Result<string>)NoteErrors.NotFound);
        var decorator = new LoggingDecorator.Query<GetNote, Result<string>>(inner, _logger);

        // Act
        var result = await decorator.HandleAsync(new GetNote(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        // Assert
        result.Error.ShouldBe(NoteErrors.NotFound);
        _logger.LatestRecord.Message.ShouldStartWith($"{nameof(GetNote)} failed with Note.NotFound (NotFound)");
    }

    [Fact]
    public async Task Exception_propagates_unchanged()
    {
        // Arrange
        var inner = Substitute.For<ICommandHandler<CreateNote, Result>>();
        inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>())
            .Returns<Result>(_ => throw new InvalidOperationException("Database unavailable."));
        var decorator = new LoggingDecorator.Command<CreateNote, Result>(inner, _logger);

        // Act
        var act = () => decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }
}
