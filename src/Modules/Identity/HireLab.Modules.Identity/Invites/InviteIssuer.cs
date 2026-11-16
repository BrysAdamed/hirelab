using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites;

internal sealed record IssuedInvite(Invite Invite, string Token)
{
    public override string ToString() => nameof(IssuedInvite);
}

/// <summary>
/// Issues an invite of any kind: the email must not belong to a user, and any open invite for the same email is revoked
/// in the same transaction, so each email has at most one open invite.
/// </summary>
internal sealed class InviteIssuer(IdentityModuleDbContext dbContext, UserManager<User> userManager, TimeProvider timeProvider)
{
    public async Task<Result<IssuedInvite>> IssueAsync(
        string email,
        string displayName,
        Func<Invitee, OneTimeToken, DateTimeOffset, Invite> create,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(create);

        var login = email.Trim();
        if (await userManager.FindByEmailAsync(login) is not null)
        {
            return InviteErrors.EmailInUse;
        }

        var invitee = new Invitee(login, userManager.NormalizeEmail(login)!, displayName);
        var now = timeProvider.GetUtcNow();
        var token = OneTimeToken.New();
        var invite = create(invitee, token, now);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var open = await dbContext.Invites
            .Where(existing => existing.NormalizedEmail == invitee.NormalizedEmail && existing.UsedAt == null && existing.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var existing in open)
        {
            existing.Revoke(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Invites.Add(invite);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IssuedInvite(invite, token.Value);
    }
}
