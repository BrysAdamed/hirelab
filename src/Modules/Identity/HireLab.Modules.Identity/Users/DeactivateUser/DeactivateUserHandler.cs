using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users.DeactivateUser;

internal sealed record DeactivateUserCommand(UserId UserId, StaffScope Scope);

/// <summary>
/// Deactivates a user in the caller's scope, never the caller. The user gets a new security stamp, so their sessions end
/// within the validation window. The pending invites and reset links they issued are revoked, so a departing manager's
/// links create no accounts and take over none, and so are the reset links for them, so none works after a
/// reactivation. Deactivating an inactive user changes nothing.
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
        var userId = user.Id;
        await dbContext.Invites.Where(invite => invite.InvitedBy == userId).RevokePendingAsync(now, cancellationToken);
        await dbContext.ResetLinks.Where(link => link.IssuedBy == userId || link.UserId == userId).RevokePendingAsync(now, cancellationToken);

        user.Deactivate();
        return await userManager.SaveAccessChangeAsync(user);
    }
}
