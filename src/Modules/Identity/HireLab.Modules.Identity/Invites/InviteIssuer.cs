using System.Linq.Expressions;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Persistence.Configurations;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites;

/// <summary>
/// Issues an invite of any kind (ADR 0021). The email must not belong to a user, and must not have a pending invite
/// outside the issuer's <see cref="StaffScope"/>. The open invites for the email are revoked in the same transaction,
/// so each email has at most one open invite; an admin invite also replaces every open admin invite.
/// </summary>
internal sealed class InviteIssuer(
    IdentityModuleDbContext dbContext,
    UserManager<User> userManager,
    OneTimeLinkIssuer issuer,
    TimeProvider timeProvider)
{
    public async Task<Result<IssuedLink<Invite>>> IssueAsync(
        string email,
        string displayName,
        StaffScope scope,
        Func<Invitee, OneTimeToken, DateTimeOffset, Invite> create,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(create);

        var login = email.Trim();
        if (await userManager.FindByEmailAsync(login) is not null)
        {
            return InviteErrors.EmailInUse;
        }

        var normalizedEmail = userManager.NormalizeEmail(login)!;
        var now = timeProvider.GetUtcNow();

        var pending = await dbContext.Invites.AsNoTracking()
            .WherePendingAt(now)
            .Where(invite => invite.NormalizedEmail == normalizedEmail)
            .Select(invite => new { invite.Role, invite.TeamId })
            .ToListAsync(cancellationToken);

        if (pending.Any(invite => !scope.Covers(invite.Role, invite.TeamId)))
        {
            return InviteErrors.PendingElsewhere;
        }

        var token = OneTimeToken.New();
        var invite = create(new Invitee(login, normalizedEmail, displayName), token, now);

        Expression<Func<Invite, bool>> replaces = invite.Role == StaffRole.Admin
            ? existing => existing.NormalizedEmail == normalizedEmail || existing.Role == StaffRole.Admin
            : existing => existing.NormalizedEmail == normalizedEmail;

        return await issuer.IssueAsync(invite, token, replaces, InviteConfiguration.OneOpenInvitePerEmailIndex, cancellationToken);
    }
}
