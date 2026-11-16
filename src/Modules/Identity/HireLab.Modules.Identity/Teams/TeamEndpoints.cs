using HireLab.Modules.Identity.Teams.ListTeams;
using HireLab.Modules.Identity.Teams.RenameTeam;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Teams;

internal static class TeamEndpoints
{
    public const string Tag = "Teams";

    public static void MapTeamEndpoints(this IEndpointRouteBuilder staff)
    {
        var teams = staff.MapGroup("/teams").WithTags(Tag).RequireAuthorization(StaffPolicies.Admin);
        teams.MapListTeams();
        teams.MapRenameTeam();
    }
}
