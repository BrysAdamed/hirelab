using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.SharedKernel.Tests.Handlers;

public sealed class HandlerRegistrationTests : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly AsyncServiceScope _scope;

    public HandlerRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<NotesUnitOfWork>();
        services.AddScoped<RecordedEvents>();
        services.AddHandlers<NotesUnitOfWork>(typeof(HandlerRegistrationTests).Assembly);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _scope = _provider.CreateAsyncScope();
    }

    [Fact]
    public async Task Command_handler_is_resolved_with_the_decorators_and_saves_then_dispatches()
    {
        // Arrange
        var handler = _scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateNote, Result<Guid>>>();
        var unitOfWork = _scope.ServiceProvider.GetRequiredService<NotesUnitOfWork>();
        var recordedEvents = _scope.ServiceProvider.GetRequiredService<RecordedEvents>();

        // Act
        var result = await handler.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        unitOfWork.Saved.ShouldContainKey(result.Value);
        recordedEvents.Events.ShouldBe([new NoteCreated(result.Value)]);
    }

    [Fact]
    public async Task Command_handler_rejects_an_invalid_command_through_the_registered_validator()
    {
        // Arrange
        var handler = _scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateNote, Result<Guid>>>();
        var unitOfWork = _scope.ServiceProvider.GetRequiredService<NotesUnitOfWork>();
        var recordedEvents = _scope.ServiceProvider.GetRequiredService<RecordedEvents>();

        // Act
        var result = await handler.HandleAsync(new CreateNote(""), TestContext.Current.CancellationToken);

        // Assert
        result.Error.ShouldBeOfType<ValidationError>().Errors.ShouldContainKey(nameof(CreateNote.Text));
        unitOfWork.Saved.ShouldBeEmpty();
        recordedEvents.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task Query_handler_is_resolved_with_validation()
    {
        // Arrange
        var handler = _scope.ServiceProvider.GetRequiredService<IQueryHandler<GetNote, Result<string>>>();

        // Act
        var invalid = await handler.HandleAsync(new GetNote(Guid.Empty), TestContext.Current.CancellationToken);
        var missing = await handler.HandleAsync(new GetNote(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        // Assert
        invalid.Error.ShouldBeOfType<ValidationError>();
        missing.Error.ShouldBe(NoteErrors.NotFound);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _provider.DisposeAsync();
    }
}
