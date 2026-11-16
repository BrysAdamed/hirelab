using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.Users;

internal static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user was not found.");

    public static readonly Error CannotDeactivateSelf = Error.Conflict("User.CannotDeactivateSelf", "You can't deactivate yourself.");

    public static readonly Error TeamHasManager = Error.Conflict(
        "User.TeamHasManager",
        "This manager's team already has another active manager. Deactivate that manager first.");

    public static readonly Error NotAnInterviewer = Error.Conflict(
        "User.NotAnInterviewer",
        "Only interviewers can move to another team. To give a team a new manager, deactivate the old one and invite a new one.");

    public static readonly Error ChangedMeanwhile = Error.Conflict(
        "User.ChangedMeanwhile",
        "Someone else changed this user at the same time. Reload and try again.");
}
