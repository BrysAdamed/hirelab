using HireLab.Modules.Identity.Users.DeactivateUser;
using HireLab.Modules.Identity.Users.ListUsers;
using HireLab.Modules.Identity.Users.ReactivateUser;
using HireLab.Modules.Identity.Users.ReassignUser;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Users;

internal static class UserEndpoints
{
    public const string Tag = "Users";

    public static void MapUserEndpoints(this IEndpointRouteBuilder staff)
    {
        var users = staff.MapGroup("/users").WithTags(Tag).RequireAuthorization(StaffPolicies.Admin);
        users.MapListUsers("ListUsers", _ => StaffScope.Everyone)
            .WithSummary("Lists every user, active and deactivated, by display name.");
        users.MapDeactivateUser("DeactivateUser", _ => StaffScope.Everyone);
        users.MapReactivateUser("ReactivateUser", _ => StaffScope.Everyone);
        users.MapReassignUser();

        var interviewers = staff.MapGroup("/my-team/interviewers").WithTags(Tag).RequireAuthorization(StaffPolicies.Manager);
        interviewers.MapListUsers("ListMyTeamInterviewers", StaffScope.InterviewersOf)
            .WithSummary("Lists the interviewers of the manager's team, active and deactivated, by display name.");
        interviewers.MapDeactivateUser("DeactivateMyTeamInterviewer", StaffScope.InterviewersOf);
        interviewers.MapReactivateUser("ReactivateMyTeamInterviewer", StaffScope.InterviewersOf);
    }
}
