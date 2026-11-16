using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.ResetLinks;

internal static class ResetLinkErrors
{
    public static readonly Error UserNotFound = Error.NotFound("ResetLink.UserNotFound", "The user was not found.");

    public static readonly Error UserInactive = Error.Conflict(
        "ResetLink.UserInactive",
        "This user is deactivated. Reactivate them before resetting their password.");

    public static readonly Error LinkNotFound = Error.NotFound(
        "ResetLink.LinkNotFound",
        "This reset link is not valid. Check that you copied the whole link, or ask for a new one.");

    /// <summary>The same code and message as <c>OneTimeLink.EnsureUsable</c>, for a redemption that lost a race.</summary>
    public static readonly Error AlreadyUsed = Error.Conflict("ResetLink.AlreadyUsed", "This reset link has already been used.");

    public static Error PasswordRejected(IEnumerable<string> reasons) =>
        Error.Validation("ResetLink.PasswordRejected", string.Join(" ", reasons));
}
