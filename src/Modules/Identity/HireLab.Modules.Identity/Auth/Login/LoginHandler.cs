using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Modules.Identity.Auth.Login;

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
        if (await userManager.FindByEmailAsync(command.Email.Trim()) is not { } user)
        {
            unknownUserPasswordCheck.Verify(userManager.PasswordHasher, command.Password);
            return InvalidCredentials;
        }

        var signIn = await signInManager.PasswordSignInAsync(user, command.Password, isPersistent: false, lockoutOnFailure: true);

        return signIn.Succeeded ? user.Id : InvalidCredentials;
    }
}
