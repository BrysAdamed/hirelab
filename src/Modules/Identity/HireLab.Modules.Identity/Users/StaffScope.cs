using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Users;

/// <summary>
/// The staff a user manages, by role and team (ADR 0020, ADR 0021): the admin manages every user, and the invites and
/// reset links of managers; a manager manages the interviewers of their own team. An empty role or team means any.
/// </summary>
internal sealed record StaffScope(StaffRole? Role, TeamId? TeamId)
{
    public static StaffScope Everyone { get; } = new(null, null);

    public static StaffScope Managers { get; } = new(StaffRole.Manager, null);

    public static StaffScope InterviewersOf(ICurrentUser manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new(StaffRole.Interviewer, manager.TeamId ?? throw new InvalidOperationException("A manager always has a team."));
    }

    public bool Covers(StaffRole role, TeamId? teamId) => (Role is null || Role == role) && (TeamId is null || TeamId == teamId);

    public IQueryable<User> Apply(IQueryable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        var inRole = Role is { } role ? users.Where(user => user.Role == role) : users;
        return TeamId is { } teamId ? inRole.Where(user => user.TeamId == teamId) : inRole;
    }

    public IQueryable<Invite> Apply(IQueryable<Invite> invites)
    {
        ArgumentNullException.ThrowIfNull(invites);

        var inRole = Role is { } role ? invites.Where(invite => invite.Role == role) : invites;
        return TeamId is { } teamId ? inRole.Where(invite => invite.TeamId == teamId) : inRole;
    }
}
