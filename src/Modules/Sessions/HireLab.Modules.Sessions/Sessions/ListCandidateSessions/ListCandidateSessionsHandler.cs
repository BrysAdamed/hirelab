using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.Sessions.ListCandidateSessions;

internal sealed record ListCandidateSessionsQuery(CandidateId CandidateId);

/// <summary>One of a candidate's sessions, shown as "Session <see cref="Number"/>".</summary>
internal sealed record CandidateSessionResponse(
    Guid Id,
    int Number,
    InterviewSessionState State,
    DateTimeOffset? ScheduledAt,
    int TaskCount,
    DateTimeOffset CreatedAt);

internal sealed class ListCandidateSessionsHandler(SessionsDbContext dbContext, ICandidateDirectory candidates)
    : IQueryHandler<ListCandidateSessionsQuery, Result<IReadOnlyList<CandidateSessionResponse>>>
{
    public async Task<Result<IReadOnlyList<CandidateSessionResponse>>> HandleAsync(
        ListCandidateSessionsQuery query,
        CancellationToken cancellationToken)
    {
        if (await candidates.FindAsync(query.CandidateId, cancellationToken) is null)
        {
            return InterviewSessionErrors.CandidateNotFound;
        }

        return await dbContext.Sessions.AsNoTracking()
            .Where(session => session.CandidateId == query.CandidateId)
            .OrderBy(session => session.Number)
            .Select(session => new CandidateSessionResponse(
                session.Id.Value,
                session.Number,
                session.State,
                session.ScheduledAt,
                session.Tasks.Count,
                session.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
