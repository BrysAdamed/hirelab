using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Invites;

/// <summary>The invites a staff user manages: the admin all manager invites, a manager the interviewer invites of their team.</summary>
internal sealed record InviteScope(StaffRole Role, TeamId? TeamId)
{
    public static InviteScope ManagerInvites { get; } = new(StaffRole.Manager, null);

    public static InviteScope InterviewerInvitesOf(ICurrentUser manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new(StaffRole.Interviewer, manager.TeamId ?? throw new InvalidOperationException("A manager always has a team."));
    }

    public IQueryable<Invite> Apply(IQueryable<Invite> invites)
    {
        ArgumentNullException.ThrowIfNull(invites);

        var inRole = invites.Where(invite => invite.Role == Role);
        return TeamId is { } teamId ? inRole.Where(invite => invite.TeamId == teamId) : inRole;
    }
}
