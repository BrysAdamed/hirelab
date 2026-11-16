using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HireLab.Testing.Positions;

/// <summary>
/// Creates job positions and candidates with obviously fake values through the Positions domain, for test assemblies
/// of modules that only hold a <see cref="CandidateId"/> (ADR 0023).
/// </summary>
public static class TestCandidates
{
    private static int _sequence;

    /// <summary>Adds an open job position to <paramref name="teamId"/> with one candidate and returns the candidate.</summary>
    public static async Task<CandidateId> CreateCandidateAsync(this HireLabTestHost host, TeamId teamId, string? fullName = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        var number = Interlocked.Increment(ref _sequence);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        var position = JobPosition.Create(
            teamId,
            new JobPositionDetails($"Fake Position {number}", "A made-up position for tests.", "- Writes fake code", null, null),
            UserId.New());
        var candidate = position.AddCandidate(
            new CandidateDetails(fullName ?? $"Fake Candidate {number}", $"candidate{number}@example.com", null, []),
            UserId.New()).Value;
        dbContext.JobPositions.Add(position);
        dbContext.Candidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return candidate.Id;
    }

    /// <summary>Anonymises a candidate whose position is open, as their manager would.</summary>
    public static async Task AnonymiseCandidateAsync(this HireLabTestHost host, CandidateId candidateId)
    {
        ArgumentNullException.ThrowIfNull(host);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        var candidate = await dbContext.Candidates.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == candidateId, cancellationToken);
        var position = await dbContext.JobPositions.IgnoreQueryFilters()
            .SingleAsync(position => position.Id == candidate.JobPositionId, cancellationToken);
        candidate.Anonymise(position, UserId.New(), host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Closes the candidate's position, which anonymises its candidates, as its manager would.</summary>
    public static async Task ClosePositionOfAsync(this HireLabTestHost host, CandidateId candidateId)
    {
        ArgumentNullException.ThrowIfNull(host);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        var positionId = await dbContext.Candidates.IgnoreQueryFilters()
            .Where(candidate => candidate.Id == candidateId)
            .Select(candidate => candidate.JobPositionId)
            .SingleAsync(cancellationToken);
        var position = await dbContext.JobPositions.IgnoreQueryFilters().SingleAsync(position => position.Id == positionId, cancellationToken);
        var candidates = await dbContext.Candidates.IgnoreQueryFilters()
            .Where(candidate => candidate.JobPositionId == positionId)
            .ToListAsync(cancellationToken);
        position.Close(candidates, UserId.New(), host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
