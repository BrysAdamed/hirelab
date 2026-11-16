using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.Sessions.Persistence;
using HireLab.SharedKernel.Events;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Sessions.CandidateLinks.IssueCandidateLink;

internal sealed record IssueCandidateLinkCommand(InterviewSessionId SessionId);

/// <summary>
/// The new candidate link's path, <c>/s/{token}</c>. It is returned only here: the token isn't stored, so the link can't
/// be shown again, and the staff app builds the absolute URL from its own origin.
/// </summary>
internal sealed record IssuedCandidateLinkResponse(string LinkPath, DateTimeOffset CreatedAt)
{
    public override string ToString() => nameof(IssuedCandidateLinkResponse);
}

/// <summary>
/// Creates or regenerates a session's candidate link (ADR 0008, ADR 0031). Regenerating revokes the old token and the
/// cookies issued from it, and raises <see cref="CandidateLinkRevoked"/> so the candidate hub drops their connections.
/// Refused for a finished session, an anonymised candidate and a closed position. Of two concurrent regenerations, the
/// slower one fails with <c>Concurrency.ChangedMeanwhile</c>.
/// </summary>
internal sealed class IssueCandidateLinkHandler(
    SessionsDbContext dbContext,
    ICandidateDirectory candidates,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IIntegrationEventQueue events)
    : ICommandHandler<IssueCandidateLinkCommand, Result<IssuedCandidateLinkResponse>>
{
    public async Task<Result<IssuedCandidateLinkResponse>> HandleAsync(IssueCandidateLinkCommand command, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions.SingleOrDefaultAsync(session => session.Id == command.SessionId, cancellationToken);
        if (session is null)
        {
            return InterviewSessionErrors.NotFound;
        }

        if (session.State == InterviewSessionState.Finished)
        {
            return InterviewSessionErrors.Finished;
        }

        var eligibility = CandidateEligibility.Check(await candidates.FindAsync(session.CandidateId, cancellationToken));
        if (eligibility.IsFailure)
        {
            return eligibility.Error;
        }

        var replaced = session.CandidateLink?.Id;
        var issued = session.IssueCandidateLink(currentUser.Id, timeProvider.GetUtcNow());
        if (issued.IsFailure)
        {
            return issued.Error;
        }

        if (replaced is { } revokedLinkId)
        {
            events.Enqueue(new CandidateLinkRevoked(session.Id, revokedLinkId));
        }

        return new IssuedCandidateLinkResponse(issued.Value.LinkPath, session.CandidateLink!.CreatedAt);
    }
}
