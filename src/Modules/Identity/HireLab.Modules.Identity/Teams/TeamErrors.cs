using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Identity.Teams;

internal static class TeamErrors
{
    public static readonly Error NotFound = Error.NotFound("Team.NotFound", "The team was not found.");
}
