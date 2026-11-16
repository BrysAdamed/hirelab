using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users.ReactivateUser;

internal sealed record ReactivateUserCommand(UserId UserId, StaffScope Scope);

/// <summary>
/// Reactivates a user in the caller's scope with a new security stamp. A manager is refused while their team has another
/// active manager. Reactivating an active user changes nothing.
/// </summary>
internal sealed class ReactivateUserHandler(IdentityModuleDbContext dbContext, UserManager<User> userManager)
    : ICommandHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> HandleAsync(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await command.Scope.Apply(dbContext.Users).SingleOrDefaultAsync(user => user.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (user.IsActive)
        {
            return Result.Success();
        }

        if (user is { Role: StaffRole.Manager, TeamId: { } teamId } && await dbContext.HasActiveManagerAsync(teamId, cancellationToken))
        {
            return UserErrors.TeamHasManager;
        }

        user.Reactivate();
        return await userManager.SaveAccessChangeAsync(user);
    }
}
