using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Users;

/// <summary>The users a staff user manages: the admin everyone, a manager the interviewers of their team.</summary>
internal sealed record UserScope(StaffRole? Role, TeamId? TeamId)
{
    public static UserScope Everyone { get; } = new(null, null);

    public static UserScope InterviewersOf(ICurrentUser manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new(StaffRole.Interviewer, manager.TeamId ?? throw new InvalidOperationException("A manager always has a team."));
    }

    public IQueryable<User> Apply(IQueryable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        var inRole = Role is { } role ? users.Where(user => user.Role == role) : users;
        return TeamId is { } teamId ? inRole.Where(user => user.TeamId == teamId) : inRole;
    }
}
