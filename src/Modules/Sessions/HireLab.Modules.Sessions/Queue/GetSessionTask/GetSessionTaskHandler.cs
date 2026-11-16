using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Queue.GetSessionTask;

internal sealed record GetSessionTaskQuery(InterviewSessionId SessionId, SessionTaskId SessionTaskId);

/// <summary>
/// A session task as the interviewer sees it while running the session: the content and staff material of the version
/// it pins, and the candidate's response with its revision (no response and revision 0 before the first save).
/// </summary>
internal sealed record SessionTaskDetailResponse(
    Guid Id,
    Guid TaskId,
    SessionTaskState State,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary,
    string ReferenceAnswer,
    string? InterviewerNotes,
    JsonElement CandidateContent,
    JsonElement StaffContent,
    JsonElement? Response,
    int ResponseRevision);

/// <summary>Reads one task of a session's queue from its snapshot, never from the task bank (ADR 0031), with its response.</summary>
internal sealed class GetSessionTaskHandler(SessionsDbContext dbContext)
    : IQueryHandler<GetSessionTaskQuery, Result<SessionTaskDetailResponse>>
{
    public async Task<Result<SessionTaskDetailResponse>> HandleAsync(GetSessionTaskQuery query, CancellationToken cancellationToken)
    {
        var task = await dbContext.Set<SessionTask>().AsNoTracking()
            .Where(task => task.SessionId == query.SessionId && task.Id == query.SessionTaskId)
            .Select(task => new SessionTaskDetailResponse(
                task.Id.Value,
                task.TaskId.Value,
                task.State,
                task.VersionNumber,
                task.TaskTypeKey,
                task.Title,
                task.Summary,
                task.ReferenceAnswer,
                task.InterviewerNotes,
                task.CandidateContent,
                task.StaffContent,
                null,
                0))
            .SingleOrDefaultAsync(cancellationToken);
        if (task is null)
        {
            var sessionExists = await dbContext.Sessions.AnyAsync(session => session.Id == query.SessionId, cancellationToken);
            return sessionExists ? InterviewSessionErrors.SessionTaskNotFound : InterviewSessionErrors.NotFound;
        }

        var response = await dbContext.Set<Response>().AsNoTracking()
            .Where(response => response.SessionTaskId == query.SessionTaskId)
            .Select(response => new StoredResponse(response.Content, response.Revision))
            .SingleOrDefaultAsync(cancellationToken);
        return response is null ? task : task with { Response = response.Content, ResponseRevision = response.Revision };
    }
}
