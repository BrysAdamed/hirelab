using HireLab.Modules.Identity.Invites.CreateInterviewerInvite;
using HireLab.Modules.Identity.Invites.CreateManagerInvite;
using HireLab.Modules.Identity.Invites.ListInvites;
using HireLab.Modules.Identity.Invites.Redemption;
using HireLab.Modules.Identity.Invites.RevokeInvite;
using HireLab.Modules.Identity.Invites.TeamsWithoutManager;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Identity.Invites;

internal static class InviteEndpoints
{
    public const string Tag = "Invites";

    public static void MapInviteEndpoints(this IEndpointRouteBuilder staff)
    {
        var invites = staff.MapGroup("/invites").WithTags(Tag);

        var managers = invites.MapGroup("/managers").RequireAuthorization(StaffPolicies.Admin);
        managers.MapListInvites("ListManagerInvites", _ => StaffScope.Managers)
            .WithSummary("Lists manager invites, newest first, until 30 days after they expire.");
        managers.MapCreateManagerInvite();
        managers.MapRevokeInvite("RevokeManagerInvite", _ => StaffScope.Managers);
        managers.MapTeamsWithoutManager();

        var interviewers = invites.MapGroup("/interviewers").RequireAuthorization(StaffPolicies.Manager);
        interviewers.MapListInvites("ListInterviewerInvites", StaffScope.InterviewersOf)
            .WithSummary("Lists the interviewer invites of the manager's team, newest first, until 30 days after they expire.");
        interviewers.MapCreateInterviewerInvite();
        interviewers.MapRevokeInvite("RevokeInterviewerInvite", StaffScope.InterviewersOf);

        invites.MapInviteRedemption();
    }
}
