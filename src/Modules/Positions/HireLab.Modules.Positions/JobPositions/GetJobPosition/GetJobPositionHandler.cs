using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.GetJobPosition;

internal sealed record GetJobPositionQuery(JobPositionId PositionId);

/// <summary>A job position with its Markdown sections. Send <see cref="Version"/> back with an edit.</summary>
internal sealed record JobPositionResponse(
    Guid Id,
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes,
    JobPositionState State,
    DateTimeOffset CreatedAt,
    uint Version);

internal sealed class GetJobPositionHandler(PositionsDbContext dbContext)
    : IQueryHandler<GetJobPositionQuery, Result<JobPositionResponse>>
{
    public async Task<Result<JobPositionResponse>> HandleAsync(GetJobPositionQuery query, CancellationToken cancellationToken)
    {
        var position = await dbContext.JobPositions.AsNoTracking()
            .Where(position => position.Id == query.PositionId)
            .Select(position => new JobPositionResponse(
                position.Id.Value,
                position.Title,
                position.Description,
                position.Requirements,
                position.NiceToHaves,
                position.InterviewerNotes,
                position.State,
                position.CreatedAt,
                position.Version))
            .SingleOrDefaultAsync(cancellationToken);

        return position is null ? JobPositionErrors.NotFound : position;
    }
}
