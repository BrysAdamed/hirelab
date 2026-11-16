using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users.DeactivateUser;

internal sealed record DeactivateUserCommand(UserId UserId, UserScope Scope);

/// <summary>
/// Deactivates a user in the caller's scope, never the caller. The user gets a new security stamp, so their sessions end
/// within the validation window, and the pending invites they issued are revoked, so a departing manager's links create
/// no accounts. Deactivating an inactive user changes nothing.
/// </summary>
internal sealed class DeactivateUserHandler(
    IdentityModuleDbContext dbContext,
    UserManager<User> userManager,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> HandleAsync(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await command.Scope.Apply(dbContext.Users).SingleOrDefaultAsync(user => user.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (user.Id == currentUser.Id)
        {
            return UserErrors.CannotDeactivateSelf;
        }

        if (!user.IsActive)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        var pendingInvites = await dbContext.Invites
            .Where(invite => invite.InvitedBy == user.Id && invite.UsedAt == null && invite.RevokedAt == null && invite.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var invite in pendingInvites)
        {
            invite.Revoke(now);
        }

        user.Deactivate();
        return await userManager.SaveAccessChangeAsync(user);
    }
}
