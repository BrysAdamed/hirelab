using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Persistence.Configurations;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users;

internal static class UserAccessChanges
{
    /// <summary>
    /// Saves a change to a user's access with a new security stamp (ADR 0020), so their sessions end at the next validation.
    /// Everything else tracked by the context is saved in the same transaction.
    /// </summary>
    public static async Task<Result> SaveAccessChangeAsync(this UserManager<User> userManager, User user)
    {
        IdentityResult updated;
        try
        {
            updated = await userManager.UpdateSecurityStampAsync(user);
        }
        catch (DbUpdateException exception) when (exception.ViolatedConstraint() == UserConfiguration.OneActiveManagerPerTeamIndex)
        {
            return UserErrors.TeamHasManager;
        }

        if (updated.Succeeded)
        {
            return Result.Success();
        }

        if (updated.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            return UserErrors.ChangedMeanwhile;
        }

        throw new InvalidOperationException($"Updating the user failed: {string.Join(", ", updated.Errors.Select(error => error.Code))}.");
    }
}
