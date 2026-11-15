using FluentValidation;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Tests.TestDoubles;

public sealed record CreateNote(string Text);

public sealed record GetNote(Guid NoteId);

public sealed record NoteCreated(Guid NoteId) : IIntegrationEvent;

public static class NoteErrors
{
    public static readonly Error NotFound = Error.NotFound("Note.NotFound", "The note was not found.");
}

internal sealed class CreateNoteValidator : AbstractValidator<CreateNote>
{
    public CreateNoteValidator()
    {
        RuleFor(command => command.Text).NotEmpty();
    }
}

internal sealed class GetNoteValidator : AbstractValidator<GetNote>
{
    public GetNoteValidator()
    {
        RuleFor(query => query.NoteId).NotEmpty();
    }
}

internal sealed class CreateNoteHandler(NotesUnitOfWork unitOfWork, IIntegrationEventQueue eventQueue)
    : ICommandHandler<CreateNote, Result<Guid>>
{
    public Task<Result<Guid>> HandleAsync(CreateNote command, CancellationToken cancellationToken)
    {
        var noteId = Guid.CreateVersion7();
        unitOfWork.Pending[noteId] = command.Text;
        eventQueue.Enqueue(new NoteCreated(noteId));
        return Task.FromResult<Result<Guid>>(noteId);
    }
}

internal sealed class GetNoteHandler(NotesUnitOfWork unitOfWork) : IQueryHandler<GetNote, Result<string>>
{
    public Task<Result<string>> HandleAsync(GetNote query, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>(
            unitOfWork.Saved.TryGetValue(query.NoteId, out var text) ? text : NoteErrors.NotFound);
}

internal sealed class NoteCreatedRecorder(RecordedEvents recordedEvents) : IIntegrationEventHandler<NoteCreated>
{
    public Task HandleAsync(NoteCreated integrationEvent, CancellationToken cancellationToken)
    {
        recordedEvents.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

internal sealed class NotesUnitOfWork : IUnitOfWork
{
    public Dictionary<Guid, string> Pending { get; } = [];

    public Dictionary<Guid, string> Saved { get; } = [];

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var count = Pending.Count;
        foreach (var (noteId, text) in Pending)
        {
            Saved[noteId] = text;
        }

        Pending.Clear();
        return Task.FromResult(count);
    }
}

internal sealed class RecordedEvents
{
    private readonly List<IIntegrationEvent> _events = [];

    public IReadOnlyList<IIntegrationEvent> Events => _events;

    public void Add(IIntegrationEvent integrationEvent) => _events.Add(integrationEvent);
}
