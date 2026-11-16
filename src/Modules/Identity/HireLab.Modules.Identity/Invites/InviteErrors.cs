using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.Invites;

internal static class InviteErrors
{
    public static readonly Error NotFound = Error.NotFound("Invite.NotFound", "The invite was not found.");

    public static readonly Error LinkNotFound = Error.NotFound(
        "Invite.LinkNotFound",
        "This invite link is not valid. Check that you copied the whole link, or ask for a new one.");

    /// <summary>The same code and message as <c>OneTimeLink.EnsureUsable</c>, for a redemption that lost a race.</summary>
    public static readonly Error AlreadyUsed = Error.Conflict("Invite.AlreadyUsed", "This invite link has already been used.");

    public static readonly Error EmailInUse = Error.Conflict("Invite.EmailInUse", "A user with this email already exists.");

    public static readonly Error TeamNotFound = Error.NotFound("Invite.TeamNotFound", "The team was not found.");

    public static readonly Error TeamHasManager = Error.Conflict(
        "Invite.TeamHasManager",
        "This team already has an active manager. Deactivate them before inviting a new one.");

    public static readonly Error AdminExists = Error.Conflict("Invite.AdminExists", "There is already an admin.");

    public static Error PasswordRejected(IEnumerable<string> reasons) =>
        Error.Validation("Invite.PasswordRejected", string.Join(" ", reasons));
}
