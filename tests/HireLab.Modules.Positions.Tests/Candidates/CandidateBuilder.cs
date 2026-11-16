using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Modules.Positions.Tests.Candidates;

/// <summary>Builds a candidate with obviously fake values. Persist it with <see cref="CandidateBuilderExtensions.AddCandidateAsync"/>.</summary>
internal sealed class CandidateBuilder
{
    private static int _sequence;

    private readonly int _number = Interlocked.Increment(ref _sequence);
    private string? _fullName;

    public bool IsAnonymised { get; private set; }

    public CandidateBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public CandidateBuilder Anonymised()
    {
        IsAnonymised = true;
        return this;
    }

    public CandidateDetails Details() => new(
        _fullName ?? $"Fake Candidate {_number}",
        $"candidate{_number}@example.com",
        "+00 000 000 000",
        [$"https://profiles.example.com/fake-{_number}"]);
}

internal static class CandidateBuilderExtensions
{
    /// <summary>Adds a candidate to an open position through the domain, as the API does.</summary>
    public static async Task<CandidateId> AddCandidateAsync(this HireLabTestHost host, JobPositionId positionId, CandidateBuilder? builder = null)
    {
        builder ??= new CandidateBuilder();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionsDbContext>();
        var position = await dbContext.JobPositions.IgnoreQueryFilters().SingleAsync(position => position.Id == positionId, cancellationToken);
        var candidate = position.AddCandidate(builder.Details(), UserId.New()).Value;
        dbContext.Candidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (builder.IsAnonymised)
        {
            candidate.Anonymise(position, UserId.New(), host.Time.GetUtcNow()).IsSuccess.ShouldBeTrue();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return candidate.Id;
    }
}
