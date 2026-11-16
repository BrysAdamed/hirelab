using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.CandidateAccess;

/// <summary>A session as its candidate link reaches it.</summary>
internal sealed record LinkedSession(InterviewSessionId SessionId, CandidateLinkId CandidateLinkId, InterviewSessionState State);

/// <summary>A session as its candidate sees it: its state and only the Current task's candidate content.</summary>
internal sealed record CandidateSessionView(InterviewSessionState State, CurrentTaskView? CurrentTask);

internal sealed record CurrentTaskView(SessionTaskId Id, string TaskTypeKey, JsonElement CandidateContent, StoredResponse? Response);

/// <summary>A task of the candidate's session, with what decides whether its response may be written.</summary>
internal sealed record CandidateSessionTask(InterviewSessionState SessionState, SessionTaskState State, string TaskTypeKey);

/// <summary>
/// The candidate's reads of their own session (ADR 0008, ADR 0009). Candidates have no team, so these bypass the team
/// filter and find the session only by the link token's hash or by both ids of the candidate cookie. They never read
/// staff content.
/// </summary>
internal sealed class CandidateSessions(SessionsDbContext dbContext) : ICandidateScope
{
    private IQueryable<InterviewSession> Sessions => dbContext.Sessions.IgnoreQueryFilters([TeamQueryFilter.Name]).AsNoTracking();

    /// <summary>Whether a session in <paramref name="state"/> gives its candidate access; <c>null</c> means the link or cookie reaches no session.</summary>
    public static Result CheckAccess(InterviewSessionState? state) => state switch
    {
        null => CandidateAccessErrors.InvalidLink,
        InterviewSessionState.Finished => CandidateAccessErrors.Ended,
        _ => Result.Success(),
    };

    public Task<LinkedSession?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = CandidateLinkToken.HashOf(token);
        return Sessions
            .Where(session => session.CandidateLink!.TokenHash == hash)
            .Select(session => new LinkedSession(session.Id, session.CandidateLink!.Id, session.State))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<InterviewSessionState?> FindStateAsync(CandidateCookie cookie, CancellationToken cancellationToken) =>
        Of(cookie)
            .Select(session => (InterviewSessionState?)session.State)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CandidateSessionView?> FindViewAsync(CandidateCookie cookie, CancellationToken cancellationToken)
    {
        var view = await Of(cookie)
            .Select(session => new CandidateSessionView(
                session.State,
                session.Tasks
                    .Where(task => task.State == SessionTaskState.Current)
                    .Select(task => new CurrentTaskView(task.Id, task.TaskTypeKey, task.CandidateContent, null))
                    .SingleOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);
        if (view?.CurrentTask is not { } current)
        {
            return view;
        }

        var response = await ResponsesOf(cookie)
            .AsNoTracking()
            .Where(response => response.SessionTaskId == current.Id)
            .Select(response => new StoredResponse(response.Content, response.Revision))
            .SingleOrDefaultAsync(cancellationToken);
        return view with { CurrentTask = current with { Response = response } };
    }

    public Task<CandidateSessionTask?> FindTaskAsync(CandidateCookie cookie, SessionTaskId sessionTaskId, CancellationToken cancellationToken) =>
        Of(cookie)
            .SelectMany(session => session.Tasks
                .Where(task => task.Id == sessionTaskId)
                .Select(task => new CandidateSessionTask(session.State, task.State, task.TaskTypeKey)))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>The response to a task of the candidate's session, tracked so that it can be saved; <c>null</c> before the first save.</summary>
    public Task<Response?> FindResponseToSaveAsync(CandidateCookie cookie, SessionTaskId sessionTaskId, CancellationToken cancellationToken) =>
        ResponsesOf(cookie).SingleOrDefaultAsync(response => response.SessionTaskId == sessionTaskId, cancellationToken);

    private IQueryable<InterviewSession> Of(CandidateCookie cookie) =>
        Sessions.Where(session => session.Id == cookie.SessionId && session.CandidateLink!.Id == cookie.CandidateLinkId);

    private IQueryable<Response> ResponsesOf(CandidateCookie cookie) =>
        dbContext.Set<Response>().IgnoreQueryFilters([TeamQueryFilter.Name]).Where(response => response.SessionId == cookie.SessionId);
}
