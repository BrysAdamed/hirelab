using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.ResetLinks;

/// <summary>The users a staff user may issue reset links for: the admin any manager, a manager the interviewers of their team.</summary>
internal sealed record ResetLinkScope(StaffRole Role, TeamId? TeamId)
{
    public static ResetLinkScope Managers { get; } = new(StaffRole.Manager, null);

    public static ResetLinkScope InterviewersOf(ICurrentUser manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new(StaffRole.Interviewer, manager.TeamId ?? throw new InvalidOperationException("A manager always has a team."));
    }

    public IQueryable<User> Apply(IQueryable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        var inRole = users.Where(user => user.Role == Role);
        return TeamId is { } teamId ? inRole.Where(user => user.TeamId == teamId) : inRole;
    }
}
