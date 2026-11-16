using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.JobPositions;

/// <summary>Builds a job position with obviously fake values. Persist it with <see cref="JobPositionBuilderExtensions.CreateJobPositionAsync"/>.</summary>
internal sealed class JobPositionBuilder
{
    private static int _sequence;

    private readonly int _number = Interlocked.Increment(ref _sequence);
    private string? _title;

    public bool IsClosed { get; private set; }

    public JobPositionBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public JobPositionBuilder Closed()
    {
        IsClosed = true;
        return this;
    }

    public JobPosition Build(TeamId teamId) => JobPosition.Create(
        teamId,
        new JobPositionDetails(
            _title ?? $"Fake Position {_number}",
            "A made-up position for tests.",
            "- Writes fake code",
            NiceToHaves: null,
            InterviewerNotes: null),
        UserId.New());
}

internal static class JobPositionBuilderExtensions
{
    public static async Task<JobPositionId> CreateJobPositionAsync(this HireLabTestHost host, TeamId teamId, JobPositionBuilder? builder = null)
    {
        builder ??= new JobPositionBuilder();
        var position = builder.Build(teamId);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        dbContext.JobPositions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (builder.IsClosed)
        {
            await host.ClosePositionAsync(position.Id);
        }

        return position.Id;
    }

    /// <summary>
    /// Closes a position through the domain as another manager would: its candidates are anonymised and their
    /// attachments deleted with their blobs.
    /// </summary>
    public static async Task ClosePositionAsync(this HireLabTestHost host, JobPositionId positionId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        var position = await dbContext.JobPositions.IgnoreQueryFilters().SingleAsync(position => position.Id == positionId, cancellationToken);
        var candidates = await dbContext.Candidates.IgnoreQueryFilters()
            .Where(candidate => candidate.JobPositionId == positionId)
            .ToListAsync(cancellationToken);
        var candidateIds = candidates.Select(candidate => candidate.Id).ToList();
        var attachments = await dbContext.Attachments.IgnoreQueryFilters()
            .Where(attachment => candidateIds.Contains(attachment.CandidateId))
            .ToListAsync(cancellationToken);

        position.Close(candidates, UserId.New(), host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue();
        dbContext.Attachments.RemoveRange(attachments);
        await dbContext.SaveChangesAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<AttachmentBlobCleanup>().DeleteAsync(attachments);
    }
}
