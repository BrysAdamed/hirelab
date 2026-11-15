using FluentValidation;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Handlers.Decorators;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Tests.TestDoubles;

namespace HireLab.SharedKernel.Tests.Handlers;

public sealed class ValidationDecoratorTests
{
    private readonly ICommandHandler<CreateNote, Result<Guid>> _inner = Substitute.For<ICommandHandler<CreateNote, Result<Guid>>>();

    [Fact]
    public async Task Valid_command_reaches_the_handler()
    {
        // Arrange
        var noteId = Guid.CreateVersion7();
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>()).Returns(Result.Success(noteId));
        var decorator = new ValidationDecorator.Command<CreateNote, Result<Guid>>(_inner, [new CreateNoteValidator()]);

        // Act
        var result = await decorator.HandleAsync(new CreateNote("Ask about caching"), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(noteId);
    }

    [Fact]
    public async Task Command_without_validators_reaches_the_handler()
    {
        // Arrange
        _inner.HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.CreateVersion7()));
        var decorator = new ValidationDecorator.Command<CreateNote, Result<Guid>>(_inner, []);

        // Act
        var result = await decorator.HandleAsync(new CreateNote(""), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Invalid_command_returns_a_validation_error_from_every_validator_and_skips_the_handler()
    {
        // Arrange
        var lengthValidator = new InlineValidator<CreateNote>();
        lengthValidator.RuleFor(command => command.Text).MinimumLength(3);
        var decorator = new ValidationDecorator.Command<CreateNote, Result<Guid>>(
            _inner,
            [new CreateNoteValidator(), lengthValidator]);

        // Act
        var result = await decorator.HandleAsync(new CreateNote(""), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Error.ShouldBeOfType<ValidationError>();
        error.Type.ShouldBe(ErrorType.Validation);
        error.Code.ShouldBe(ValidationError.DefaultCode);
        error.Errors[nameof(CreateNote.Text)].Length.ShouldBe(2);
        await _inner.DidNotReceive().HandleAsync(Arg.Any<CreateNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_query_returns_a_validation_error_for_a_result_without_value()
    {
        // Arrange
        var inner = Substitute.For<IQueryHandler<GetNote, Result>>();
        var decorator = new ValidationDecorator.Query<GetNote, Result>(inner, [new GetNoteValidator()]);

        // Act
        var result = await decorator.HandleAsync(new GetNote(Guid.Empty), TestContext.Current.CancellationToken);

        // Assert
        result.Error.ShouldBeOfType<ValidationError>().Errors.ShouldContainKey(nameof(GetNote.NoteId));
        await inner.DidNotReceive().HandleAsync(Arg.Any<GetNote>(),Arg.Any<CancellationToken>());
    }
}
