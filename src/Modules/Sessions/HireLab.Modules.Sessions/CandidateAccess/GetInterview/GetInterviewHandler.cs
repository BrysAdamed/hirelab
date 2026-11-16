using System.Text.Json;
using System.Text.Json.Serialization;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.CandidateAccess.GetInterview;

internal sealed record GetInterviewQuery(CandidateCookie Cookie);

/// <summary>
/// The page the candidate sees: the waiting page before the session goes Live, the current task while one is Current,
/// and the closing screen once every task is done.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InterviewStage>))]
internal enum InterviewStage
{
    Waiting = 1,
    CurrentTask = 2,
    Closing = 3,
}

/// <summary>
/// The candidate's interview: the stage and, only at <see cref="InterviewStage.CurrentTask"/>, the current task. Never
/// the queue, its length or other tasks' titles (ADR 0031).
/// </summary>
internal sealed record InterviewResponse(InterviewStage Stage, CurrentTaskResponse? CurrentTask);

/// <summary>
/// The current task as the candidate sees it: its id, its task type key, which picks the type's <c>candidate</c>
/// component, its candidate content, and the response saved so far with its revision (no response and revision 0 before
/// the first save). Never staff content or the title, which is written for staff.
/// </summary>
internal sealed record CurrentTaskResponse(Guid Id, string TaskType, JsonElement Content, JsonElement? Response, int Revision);

/// <summary>Reads the candidate's own session from the cookie's claims (ADR 0008), never from the route.</summary>
internal sealed class GetInterviewHandler(CandidateSessions sessions) : IQueryHandler<GetInterviewQuery, Result<InterviewResponse>>
{
    public async Task<Result<InterviewResponse>> HandleAsync(GetInterviewQuery query, CancellationToken cancellationToken)
    {
        var session = await sessions.FindViewAsync(query.Cookie, cancellationToken);
        var access = CandidateSessions.CheckAccess(session?.State);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return session! switch
        {
            { State: InterviewSessionState.Planned } => new InterviewResponse(InterviewStage.Waiting, null),
            { CurrentTask: { } task } => new InterviewResponse(
                InterviewStage.CurrentTask,
                new CurrentTaskResponse(
                    task.Id.Value,
                    task.TaskTypeKey,
                    task.CandidateContent,
                    task.Response?.Content,
                    task.Response?.Revision ?? 0)),
            _ => new InterviewResponse(InterviewStage.Closing, null),
        };
    }
}
