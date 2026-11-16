using System.Text;
using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.Modules.Sessions.Persistence.Configurations;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.CandidateAccess.SaveResponse;

internal sealed record SaveResponseCommand(CandidateCookie Cookie, SessionTaskId SessionTaskId, JsonElement Response, int Revision)
{
    public override string ToString() => nameof(SaveResponseCommand);
}

/// <summary>The response's revision after the save, which the next save names.</summary>
internal sealed record SavedResponse(int Revision);

/// <summary>
/// Saves the candidate's whole response to the Current task of their Live session (ADR 0031). The task type validates
/// it and it is stored as the task type serialises it. A save based on an older revision, or one that loses a race with
/// another tab, is refused with <c>Response.StaleRevision</c>.
/// </summary>
internal sealed class SaveResponseHandler(
    CandidateSessions sessions,
    SessionsDbContext dbContext,
    ITaskTypeRegistry taskTypes,
    IIntegrationEventQueue events)
    : ICommandHandler<SaveResponseCommand, Result<SavedResponse>>
{
    public async Task<Result<SavedResponse>> HandleAsync(SaveResponseCommand command, CancellationToken cancellationToken)
    {
        var task = await sessions.FindTaskAsync(command.Cookie, command.SessionTaskId, cancellationToken);
        if (task is null)
        {
            return InterviewSessionErrors.SessionTaskNotFound;
        }

        if (task.SessionState != InterviewSessionState.Live)
        {
            return InterviewSessionErrors.NotLive;
        }

        if (task.State != SessionTaskState.Current)
        {
            return InterviewSessionErrors.TaskNotCurrent;
        }

        if (Encoding.UTF8.GetByteCount(command.Response.GetRawText()) > Response.MaxBytes)
        {
            return ResponseErrors.TooLarge;
        }

        if (taskTypes.Find(task.TaskTypeKey) is not { } taskType)
        {
            return ResponseErrors.TaskTypeNotRegistered;
        }

        var valid = taskType.ValidateResponse(command.Response);
        if (valid.IsFailure)
        {
            return valid.Error;
        }

        var response = await sessions.FindResponseToSaveAsync(command.Cookie, command.SessionTaskId, cancellationToken);
        if (response is null)
        {
            response = Response.Start(command.Cookie.SessionId, command.SessionTaskId);
            dbContext.Add(response);
        }

        var saved = response.Save(Serialise(taskType, command.Response), taskType.ResponseSchemaVersion, command.Revision);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsLostRace(exception))
        {
            dbContext.ChangeTracker.Clear();
            return ResponseErrors.StaleRevision;
        }

        events.Enqueue(new ResponseUpdated(response.SessionId, response.SessionTaskId, response.Revision));
        return new SavedResponse(response.Revision);
    }

    private static JsonElement Serialise(ITaskType taskType, JsonElement response) =>
        JsonSerializer.SerializeToElement(
            response.Deserialize(taskType.ResponseType, TaskTypeJson.Options),
            taskType.ResponseType,
            TaskTypeJson.Options);

    private static bool IsLostRace(DbUpdateException exception) =>
        exception is DbUpdateConcurrencyException || exception.ViolatedConstraint() == ResponseConfiguration.PrimaryKey;
}
