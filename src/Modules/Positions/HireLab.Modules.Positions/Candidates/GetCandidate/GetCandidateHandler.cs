using HireLab.Modules.Identity.Contracts;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates.GetCandidate;

internal sealed record GetCandidateQuery(CandidateId CandidateId);

/// <summary>
/// A candidate's profile with their position's title and state. <see cref="DisplayName"/> is how to show them;
/// <see cref="FullName"/> and the contact details are empty once they are anonymised, and <see cref="AnonymisedAt"/>
/// says when (<see cref="AnonymisedByName"/> is <c>null</c> when the user is unknown). Send <see cref="Version"/> back
/// with an edit.
/// </summary>
internal sealed record CandidateResponse(
    Guid Id,
    Guid PositionId,
    string PositionTitle,
    JobPositionState PositionState,
    int Number,
    string DisplayName,
    string? FullName,
    string? Email,
    string? Phone,
    IReadOnlyList<string> Links,
    bool IsAnonymised,
    DateTimeOffset? AnonymisedAt,
    Guid? AnonymisedById,
    string? AnonymisedByName,
    DateTimeOffset CreatedAt,
    uint Version);

internal sealed class GetCandidateHandler(PositionsDbContext dbContext, IStaffDirectory staffDirectory)
    : IQueryHandler<GetCandidateQuery, Result<CandidateResponse>>
{
    public async Task<Result<CandidateResponse>> HandleAsync(GetCandidateQuery query, CancellationToken cancellationToken)
    {
        var found = await dbContext.Candidates.AsNoTracking()
            .Where(candidate => candidate.Id == query.CandidateId)
            .Join(
                dbContext.JobPositions,
                candidate => candidate.JobPositionId,
                position => position.Id,
                (candidate, position) => new { Candidate = candidate, position.Title, position.State })
            .SingleOrDefaultAsync(cancellationToken);
        if (found is null)
        {
            return CandidateErrors.NotFound;
        }

        var candidate = found.Candidate;
        string? anonymisedByName = null;
        if (candidate.AnonymisedBy is { } anonymisedBy)
        {
            var names = await staffDirectory.FindDisplayNamesAsync([anonymisedBy], cancellationToken);
            anonymisedByName = names.GetValueOrDefault(anonymisedBy);
        }

        return new CandidateResponse(
            candidate.Id.Value,
            candidate.JobPositionId.Value,
            found.Title,
            found.State,
            candidate.Number,
            candidate.DisplayName,
            candidate.FullName,
            candidate.Email,
            candidate.Phone,
            candidate.Links,
            candidate.IsAnonymised,
            candidate.AnonymisedAt,
            candidate.AnonymisedBy?.Value,
            anonymisedByName,
            candidate.CreatedAt,
            candidate.Version);
    }
}
