using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.ResetLinks;

internal sealed record IssuedResetLink(ResetLink Link, string Token)
{
    public string LinkPath => ResetLinkPaths.PathFor(Token);

    public override string ToString() => nameof(IssuedResetLink);
}

/// <summary>
/// Issues a reset link for an active user. Any open reset link of that user is revoked in the same transaction, so each
/// user has at most one open link. Who may issue a link for whom is checked by the caller.
/// </summary>
internal sealed class ResetLinkIssuer(IdentityModuleDbContext dbContext, TimeProvider timeProvider)
{
    /// <param name="userId">The user whose password the link resets.</param>
    /// <param name="issuedBy">The issuing user, or <c>null</c> when the system issues the link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Result<IssuedResetLink>> IssueAsync(UserId userId, UserId? issuedBy, CancellationToken cancellationToken)
    {
        var isActive = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => (bool?)user.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (isActive is null)
        {
            return ResetLinkErrors.UserNotFound;
        }

        if (isActive is false)
        {
            return ResetLinkErrors.UserInactive;
        }

        var now = timeProvider.GetUtcNow();
        var token = OneTimeToken.New();
        var link = ResetLink.For(userId, issuedBy, token, now);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var open = await dbContext.ResetLinks
            .Where(existing => existing.UserId == userId && existing.UsedAt == null && existing.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var existing in open)
        {
            existing.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ResetLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IssuedResetLink(link, token.Value);
    }
}
