using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.Redemption;

internal static class RedeemableInvites
{
    /// <summary>Finds the invite by its link token and checks it can still be redeemed.</summary>
    public static async Task<Result<Invite>> FindRedeemableAsync(
        this IdentityModuleDbContext dbContext,
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hash = OneTimeToken.HashOf(token);
        var invite = await dbContext.Invites.SingleOrDefaultAsync(invite => invite.TokenHash == hash, cancellationToken);
        if (invite is null)
        {
            return InviteErrors.LinkNotFound;
        }

        var usable = invite.EnsureUsable(now);
        return usable.IsSuccess ? invite : usable.Error;
    }
}
