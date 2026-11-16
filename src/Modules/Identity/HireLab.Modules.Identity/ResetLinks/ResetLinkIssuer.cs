using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Persistence.Configurations;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.ResetLinks;

/// <summary>
/// Issues a reset link for an active user. Any open reset link of that user is revoked in the same transaction, so each
/// user has at most one open link. Who may issue a link for whom is checked by the caller.
/// </summary>
internal sealed class ResetLinkIssuer(IdentityModuleDbContext dbContext, OneTimeLinkIssuer issuer, TimeProvider timeProvider)
{
    /// <param name="userId">The user whose password the link resets.</param>
    /// <param name="issuedBy">The issuing user, or <c>null</c> when the system issues the link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Result<IssuedLink<ResetLink>>> IssueAsync(UserId userId, UserId? issuedBy, CancellationToken cancellationToken)
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

        var token = OneTimeToken.New();
        var link = ResetLink.For(userId, issuedBy, token, timeProvider.GetUtcNow());

        return await issuer.IssueAsync(
            link,
            token,
            existing => existing.UserId == userId,
            ResetLinkConfiguration.OneOpenLinkPerUserIndex,
            cancellationToken);
    }
}
