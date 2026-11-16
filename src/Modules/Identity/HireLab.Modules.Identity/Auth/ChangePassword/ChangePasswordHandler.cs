using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Modules.Identity.Auth.ChangePassword;

internal sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
{
    public override string ToString() => nameof(ChangePasswordCommand);
}

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty();
        RuleFor(command => command.NewPassword).NotEmpty();
    }
}

/// <summary>
/// Changes the signed-in user's password. The security stamp changes, which ends their other sessions; this session is
/// signed in again with the new stamp so it keeps working.
/// </summary>
internal sealed class ChangePasswordHandler(UserManager<User> userManager, SignInManager<User> signInManager, ICurrentUser currentUser)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public static readonly Error WrongCurrentPassword = Error.Validation(
        "Auth.WrongCurrentPassword",
        "The current password is incorrect.");

    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id.ToString())
            ?? throw new InvalidOperationException("The signed-in user does not exist.");

        var changed = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!changed.Succeeded)
        {
            return changed.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? WrongCurrentPassword
                : Error.Validation("Auth.PasswordRejected", string.Join(" ", changed.Errors.Select(error => error.Description)));
        }

        await signInManager.RefreshSignInAsync(user);
        return Result.Success();
    }
}
