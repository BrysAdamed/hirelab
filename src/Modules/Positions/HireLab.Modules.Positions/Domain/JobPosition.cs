using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// An opening a team is hiring for. A manager creates and edits it while it is <see cref="JobPositionState.Open"/>;
/// a <see cref="JobPositionState.Closed"/> position is read-only.
/// </summary>
internal sealed class JobPosition : ITeamOwned
{
    private JobPosition()
    {
    }

    public JobPositionId Id { get; private set; }

    public TeamId TeamId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Requirements { get; private set; } = string.Empty;

    public string? NiceToHaves { get; private set; }

    public string? InterviewerNotes { get; private set; }

    public JobPositionState State { get; private set; }

    public UserId CreatedBy { get; private set; }

    /// <summary>The audit timestamp (ADR 0004), set when the position is first saved.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// The number the latest candidate got. Adding a candidate raises it, which changes the position's version: two
    /// concurrent adds, or an add racing a close, cannot both be saved.
    /// </summary>
    public int LastCandidateNumber { get; private set; }

    public uint Version { get; private set; }

    public bool IsClosed => State == JobPositionState.Closed;

    public static JobPosition Create(TeamId teamId, JobPositionDetails details, UserId createdBy)
    {
        ArgumentNullException.ThrowIfNull(details);

        var position = new JobPosition
        {
            Id = JobPositionId.New(),
            TeamId = teamId,
            State = JobPositionState.Open,
            CreatedBy = createdBy,
        };
        position.Apply(details);
        return position;
    }

    public Result Edit(JobPositionDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (IsClosed)
        {
            return JobPositionErrors.Closed;
        }

        Apply(details);
        return Result.Success();
    }

    /// <summary>Adds a candidate with the position's next number. Only while the position is open.</summary>
    public Result<Candidate> AddCandidate(CandidateDetails details, UserId addedBy)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (IsClosed)
        {
            return JobPositionErrors.Closed;
        }

        LastCandidateNumber++;
        return Candidate.Create(this, LastCandidateNumber, details, addedBy);
    }

    public Result EnsureCanBeDeleted(bool hasCandidates)
    {
        if (IsClosed)
        {
            return JobPositionErrors.Closed;
        }

        return hasCandidates ? JobPositionErrors.HasCandidates : Result.Success();
    }

    private void Apply(JobPositionDetails details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Requirements);

        var normalised = details.Normalised();
        Title = normalised.Title;
        Description = normalised.Description;
        Requirements = normalised.Requirements;
        NiceToHaves = normalised.NiceToHaves;
        InterviewerNotes = normalised.InterviewerNotes;
    }
}
