using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Authentication;

internal sealed class StaffUserManager(
    IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger)
    : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    /// <summary>
    /// Sets a new password without the current one or a token (a redeemed reset link vouches for the user), clears the
    /// lockout and changes the security stamp, in one save together with anything else the context tracks.
    /// </summary>
    public async Task<IdentityResult> ReplacePasswordAsync(User user, string newPassword)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        var hashed = await UpdatePasswordHash(user, newPassword, validatePassword: true);
        if (!hashed.Succeeded)
        {
            return hashed;
        }

        var lockout = (IUserLockoutStore<User>)Store;
        await lockout.ResetAccessFailedCountAsync(user, CancellationToken);
        await lockout.SetLockoutEndDateAsync(user, lockoutEnd: null, CancellationToken);

        return await UpdateUserAsync(user);
    }
}
