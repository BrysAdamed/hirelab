using FluentValidation;
using HireLab.Modules.Identity.Authentication;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Modules.Identity.ResetLinks.Redemption;

internal sealed record ResetPasswordCommand(string Token, string Password)
{
    public override string ToString() => nameof(ResetPasswordCommand);
}

internal sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(OneTimeToken.MaxLength);
        RuleFor(command => command.Password).NotEmpty();
    }
}

/// <summary>
/// Redeems a reset link: sets the new password, clears the lockout and changes the security stamp, which ends the user's
/// existing sessions. The link and the user are saved together; the link's concurrency token stops a second redemption.
/// </summary>
internal sealed class ResetPasswordHandler(IdentityModuleDbContext dbContext, StaffUserManager userManager, TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var found = await dbContext.FindUsableResetLinkAsync(command.Token, now, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var (link, user) = found.Value;
        var used = link.Use(now);
        if (used.IsFailure)
        {
            return used.Error;
        }

        var replaced = await userManager.ReplacePasswordAsync(user, command.Password);
        if (replaced.Succeeded)
        {
            return Result.Success();
        }

        if (replaced.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            return ResetLinkErrors.AlreadyUsed;
        }

        return ResetLinkErrors.PasswordRejected(replaced.Errors.Select(error => error.Description));
    }
}
