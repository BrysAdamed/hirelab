using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// A person applying to one job position; the same person applying again is a new candidate. Added through
/// <see cref="JobPosition.AddCandidate"/>, which gives them the position's next <see cref="Number"/>. Their contact
/// details are the only personal data in the system (ADR 0023): anonymisation clears them and the candidate is shown
/// as "Candidate #N".
/// </summary>
internal sealed class Candidate : ITeamOwned
{
    private Candidate()
    {
    }

    public CandidateId Id { get; private set; }

    /// <summary>The team of the position, copied on add so the team query filter covers candidates directly (ADR 0009).</summary>
    public TeamId TeamId { get; private set; }

    public JobPositionId JobPositionId { get; private set; }

    /// <summary>The candidate's place in the position's sequence (1, 2, 3 …), never changed or reused.</summary>
    public int Number { get; private set; }

    /// <summary>The full name; <c>null</c> once the candidate is anonymised.</summary>
    public string? FullName { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string[] Links { get; private set; } = [];

    public UserId CreatedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the candidate is first saved.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public UserId? AnonymisedBy { get; private set; }

    public DateTimeOffset? AnonymisedAt { get; private set; }

    public uint Version { get; private set; }

    public bool IsAnonymised => AnonymisedAt is not null;

    /// <summary>How the candidate is shown everywhere: the full name, or "Candidate #N" once anonymised.</summary>
    public string DisplayName => IsAnonymised || FullName is null ? $"Candidate #{Number}" : FullName;

    public static Candidate Create(JobPosition position, int number, CandidateDetails details, UserId createdBy)
    {
        var candidate = new Candidate
        {
            Id = CandidateId.New(),
            TeamId = position.TeamId,
            JobPositionId = position.Id,
            Number = number,
            CreatedBy = createdBy,
        };
        candidate.Apply(details);
        return candidate;
    }

    /// <summary>Replaces the contact details. Only while <paramref name="position"/>, the candidate's position, is open.</summary>
    public Result Edit(CandidateDetails details, JobPosition position)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(position);
        if (position.Id != JobPositionId)
        {
            throw new ArgumentException("The position is not the candidate's.", nameof(position));
        }

        if (position.IsClosed)
        {
            return JobPositionErrors.Closed;
        }

        if (IsAnonymised)
        {
            return CandidateErrors.Anonymised;
        }

        Apply(details);
        return Result.Success();
    }

    private void Apply(CandidateDetails details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(details.FullName);

        var normalised = details.Normalised();
        FullName = normalised.FullName;
        Email = normalised.Email;
        Phone = normalised.Phone;
        Links = [.. normalised.Links];
    }
}
