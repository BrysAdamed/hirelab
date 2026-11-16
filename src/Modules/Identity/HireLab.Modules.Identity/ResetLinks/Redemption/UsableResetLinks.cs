using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
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
        var found = await dbContext.ResetLinks.FindUsableAsync(token, now, ResetLinkErrors.LinkNotFound, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var link = found.Value;
        var user = await dbContext.Users.SingleAsync(user => user.Id == link.UserId, cancellationToken);
        return user.IsActive ? new UsableResetLink(link, user) : ResetLinkErrors.UserInactive;
    }
}
