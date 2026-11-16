using HireLab.Modules.Identity.Contracts;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.GetJobPosition;

internal sealed record GetJobPositionQuery(JobPositionId PositionId);

/// <summary>
/// A job position with its Markdown sections. Send <see cref="Version"/> back with an edit. <see cref="ClosedAt"/> is
/// set once the position is closed; <see cref="ClosedByName"/> is <c>null</c> when it is open or the user is unknown.
/// </summary>
internal sealed record JobPositionResponse(
    Guid Id,
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes,
    JobPositionState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    Guid? ClosedById,
    string? ClosedByName,
    uint Version);

internal sealed class GetJobPositionHandler(PositionsDbContext dbContext, IStaffDirectory staffDirectory)
    : IQueryHandler<GetJobPositionQuery, Result<JobPositionResponse>>
{
    public async Task<Result<JobPositionResponse>> HandleAsync(GetJobPositionQuery query, CancellationToken cancellationToken)
    {
        var position = await dbContext.JobPositions.AsNoTracking()
            .SingleOrDefaultAsync(position => position.Id == query.PositionId, cancellationToken);
        if (position is null)
        {
            return JobPositionErrors.NotFound;
        }

        string? closedByName = null;
        if (position.ClosedBy is { } closedBy)
        {
            var names = await staffDirectory.FindDisplayNamesAsync([closedBy], cancellationToken);
            closedByName = names.GetValueOrDefault(closedBy);
        }

        return new JobPositionResponse(
            position.Id.Value,
            position.Title,
            position.Description,
            position.Requirements,
            position.NiceToHaves,
            position.InterviewerNotes,
            position.State,
            position.CreatedAt,
            position.ClosedAt,
            position.ClosedBy?.Value,
            closedByName,
            position.Version);
    }
}
