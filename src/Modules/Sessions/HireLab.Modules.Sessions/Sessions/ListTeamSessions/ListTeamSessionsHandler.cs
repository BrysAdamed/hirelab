using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.ListTeamSessions;

internal sealed record ListTeamSessionsQuery;

/// <summary>An unfinished session of the team with its candidate's current display name (ADR 0023).</summary>
internal sealed record TeamSessionResponse(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    int TaskCount,
    Guid CandidateId,
    string CandidateDisplayName);

/// <summary>
/// The team's Planned and Live sessions, soonest first: Live sessions, then planned ones by their schedule, unscheduled
/// ones last in the order they were created.
/// </summary>
internal sealed class ListTeamSessionsHandler(SessionsDbContext dbContext, ICandidateDirectory candidates)
    : IQueryHandler<ListTeamSessionsQuery, Result<IReadOnlyList<TeamSessionResponse>>>
{
    public async Task<Result<IReadOnlyList<TeamSessionResponse>>> HandleAsync(ListTeamSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await dbContext.Sessions.AsNoTracking()
            .Where(session => session.State != InterviewSessionState.Finished)
            .OrderBy(session => session.State == InterviewSessionState.Live ? 0 : 1)
            .ThenBy(session => session.ScheduledAt == null)
            .ThenBy(session => session.ScheduledAt)
            .ThenBy(session => session.CreatedAt)
            .Select(session => new
            {
                session.Id,
                session.Number,
                session.State,
                session.ScheduledAt,
                TaskCount = session.Tasks.Count,
                session.CandidateId,
            })
            .ToListAsync(cancellationToken);

        var summaries = await candidates.FindManyAsync([.. sessions.Select(session => session.CandidateId).Distinct()], cancellationToken);
        return sessions
            .Select(session => new TeamSessionResponse(
                session.Id.Value,
                session.Number,
                session.State,
                session.ScheduledAt,
                session.TaskCount,
                session.CandidateId.Value,
                summaries.GetValueOrDefault(session.CandidateId)?.DisplayName ?? CandidateNames.Unknown))
            .ToList();
    }
}
