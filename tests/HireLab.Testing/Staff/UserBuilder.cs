using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Testing.Staff;

/// <summary>
/// Builds a staff user with obviously fake values (<c>example.com</c> emails, a shared test password). Persist it with
/// <see cref="HireLabTestHost.CreateUserAsync"/>.
/// </summary>
public sealed class UserBuilder
{
    /// <summary>The password every built user gets unless <see cref="WithPassword"/> sets another.</summary>
    public const string DefaultPassword = "test-password-not-a-secret";

    private static int _sequence;

    private readonly StaffRole _role;
    private readonly TeamId? _teamId;
    private readonly int _number = Interlocked.Increment(ref _sequence);
    private string? _email;
    private string? _displayName;
    private bool _isActive = true;

    private UserBuilder(StaffRole role, TeamId? teamId)
    {
        _role = role;
        _teamId = teamId;
    }

    internal string Password { get; private set; } = DefaultPassword;

    /// <summary>The admin, who has no team.</summary>
    public static UserBuilder Admin() => new(StaffRole.Admin, teamId: null);

    /// <summary>A manager of <paramref name="teamId"/>.</summary>
    public static UserBuilder Manager(TeamId teamId) => new(StaffRole.Manager, teamId);

    /// <summary>An interviewer in <paramref name="teamId"/>.</summary>
    public static UserBuilder Interviewer(TeamId teamId) => new(StaffRole.Interviewer, teamId);

    /// <summary>Sets the user's email, which is also their login.</summary>
    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>Sets the user's display name.</summary>
    public UserBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <summary>Sets the user's password.</summary>
    public UserBuilder WithPassword(string password)
    {
        Password = password;
        return this;
    }

    /// <summary>Makes the user deactivated.</summary>
    public UserBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    internal User Build()
    {
        var email = _email ?? $"{_role.ToString().ToLowerInvariant()}{_number}@example.com";
        var displayName = _displayName ?? $"Test {_role} {_number}";

        var user = _role switch
        {
            StaffRole.Admin => User.CreateAdmin(email, displayName),
            StaffRole.Manager => User.CreateManager(email, displayName, _teamId!.Value),
            _ => User.CreateInterviewer(email, displayName, _teamId!.Value),
        };

        if (!_isActive)
        {
            user.Deactivate();
        }

        return user;
    }
}
