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
        users.MapListUsers("ListUsers", _ => UserScope.Everyone)
            .WithSummary("Lists every user, active and deactivated, by display name.");
        users.MapDeactivateUser("DeactivateUser", _ => UserScope.Everyone);
        users.MapReactivateUser("ReactivateUser", _ => UserScope.Everyone);
        users.MapReassignUser();

        var interviewers = staff.MapGroup("/my-team/interviewers").WithTags(Tag).RequireAuthorization(StaffPolicies.Manager);
        interviewers.MapListUsers("ListMyTeamInterviewers", UserScope.InterviewersOf)
            .WithSummary("Lists the interviewers of the manager's team, active and deactivated, by display name.");
        interviewers.MapDeactivateUser("DeactivateMyTeamInterviewer", UserScope.InterviewersOf);
        interviewers.MapReactivateUser("ReactivateMyTeamInterviewer", UserScope.InterviewersOf);
    }
}
