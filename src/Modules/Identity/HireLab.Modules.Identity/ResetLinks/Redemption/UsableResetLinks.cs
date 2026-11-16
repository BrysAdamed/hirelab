using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.ResetLinks.Redemption;

internal sealed record UsableResetLink(ResetLink Link, User User);

internal static class UsableResetLinks
{
    /// <summary>Finds the reset link by its token and its user, and checks the link can still be used by an active user.</summary>
    public static async Task<Result<UsableResetLink>> FindUsableResetLinkAsync(
        this IdentityModuleDbContext dbContext,
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hash = OneTimeToken.HashOf(token);
        var link = await dbContext.ResetLinks.SingleOrDefaultAsync(link => link.TokenHash == hash, cancellationToken);
        if (link is null)
        {
            return ResetLinkErrors.LinkNotFound;
        }

        var usable = link.EnsureUsable(now);
        if (usable.IsFailure)
        {
            return usable.Error;
        }

        var user = await dbContext.Users.SingleAsync(user => user.Id == link.UserId, cancellationToken);
        return user.IsActive ? new UsableResetLink(link, user) : ResetLinkErrors.UserInactive;
    }
}
