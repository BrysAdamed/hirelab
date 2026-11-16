using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;

namespace HireLab.Modules.Identity.Domain;

internal sealed class User : IdentityUser<UserId>
{
    public const int DisplayNameMaxLength = 100;

    private User()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;

    public StaffRole Role { get; private set; }

    public TeamId? TeamId { get; private set; }

    public bool IsActive { get; private set; }

    public static User CreateAdmin(string email, string displayName) => Create(email, displayName, StaffRole.Admin, teamId: null);

    public static User CreateManager(string email, string displayName, TeamId teamId) =>
        Create(email, displayName, StaffRole.Manager, teamId);

    public static User CreateInterviewer(string email, string displayName, TeamId teamId) =>
        Create(email, displayName, StaffRole.Interviewer, teamId);

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private static User Create(string email, string displayName, StaffRole role, TeamId? teamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var login = email.Trim();

        return new User
        {
            Id = UserId.New(),
            UserName = login,
            Email = login,
            DisplayName = displayName.Trim(),
            Role = role,
            TeamId = teamId,
            IsActive = true,
        };
    }
}
