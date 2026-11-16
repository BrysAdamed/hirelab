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
            // TODO HLB-43: close the position through the domain once closing exists.
            await dbContext.Database.ExecuteSqlAsync(
                $"UPDATE positions.job_positions SET state = 'Closed' WHERE id = {position.Id.Value}",
                cancellationToken);
        }

        return position.Id;
    }
}
