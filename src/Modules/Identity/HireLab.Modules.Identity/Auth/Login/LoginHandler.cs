using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Modules.Identity.Auth.Login;

/// <summary>
/// Logs a staff user in. Every refusal costs exactly one password hash verification, whether the email is unknown, the
/// account is deactivated or locked out, or the password is wrong, so response times don't tell them apart.
/// </summary>
internal sealed class LoginHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    UnknownUserPasswordCheck unknownUserPasswordCheck)
    : ICommandHandler<LoginCommand, Result<UserId>>
{
    public static readonly Error InvalidCredentials = Error.Validation(
        "Auth.InvalidCredentials",
        "The email or password is incorrect, or the account can't log in right now. After 5 failed attempts an account is locked for 15 minutes.");

    public async Task<Result<UserId>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null || !await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
        {
            unknownUserPasswordCheck.Verify(userManager.PasswordHasher, command.Password);
            return InvalidCredentials;
        }

        var signIn = await signInManager.PasswordSignInAsync(user, command.Password, isPersistent: false, lockoutOnFailure: true);

        return signIn.Succeeded ? user.Id : InvalidCredentials;
    }
}
