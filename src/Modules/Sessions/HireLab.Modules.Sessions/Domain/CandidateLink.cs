using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// The current candidate link of an <see cref="InterviewSession"/> (ADR 0008, ADR 0031). Only the hash of its token is
/// stored, so the link can't be shown again; regenerating replaces it with a new one, with a new <see cref="Id"/>, which
/// revokes the old token and the cookies issued from it.
/// </summary>
internal sealed class CandidateLink
{
    private CandidateLink()
    {
    }

    public CandidateLinkId Id { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public UserId CreatedBy { get; private set; }

    internal static CandidateLink Issue(CandidateLinkToken token, UserId createdBy, DateTimeOffset now) =>
        new()
        {
            Id = CandidateLinkId.New(),
            TokenHash = token.Hash,
            CreatedAt = now,
            CreatedBy = createdBy,
        };
}
