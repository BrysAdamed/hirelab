using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Domain;

/// <summary>The person an invite is for, as the inviter entered them.</summary>
internal sealed record Invitee(string Email, string NormalizedEmail, string DisplayName)
{
    public override string ToString() => nameof(Invitee);
}

/// <summary>
/// An invite link (ADR 0007, ADR 0020): redeeming it creates a user with a fixed role and team. A manager invite either
/// names a new team, created only on redemption, or an existing team without an active manager (handover).
/// </summary>
internal sealed class Invite : OneTimeLink
{
    public const int EmailMaxLength = 256;

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(72);
    public static readonly TimeSpan AdminLifetime = TimeSpan.FromHours(24);

    private Invite()
    {
    }

    private Invite(
        Invitee invitee,
        StaffRole role,
        TeamId? teamId,
        string? newTeamName,
        UserId? invitedBy,
        OneTimeToken token,
        DateTimeOffset now,
        TimeSpan lifetime)
        : base(token, now, lifetime)
    {
        ArgumentNullException.ThrowIfNull(invitee);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitee.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitee.NormalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitee.DisplayName);

        Id = InviteId.New();
        Email = invitee.Email.Trim();
        NormalizedEmail = invitee.NormalizedEmail;
        DisplayName = invitee.DisplayName.Trim();
        Role = role;
        TeamId = teamId;
        NewTeamName = newTeamName?.Trim();
        InvitedBy = invitedBy;
    }

    public InviteId Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public StaffRole Role { get; private set; }

    /// <summary>The existing team the invitee joins; <c>null</c> for a new-team manager invite and for the admin.</summary>
    public TeamId? TeamId { get; private set; }

    /// <summary>The team a manager invite creates on redemption.</summary>
    public string? NewTeamName { get; private set; }

    /// <summary>The user who issued the invite; <c>null</c> for the admin bootstrap invite.</summary>
    public UserId? InvitedBy { get; private set; }

    protected override string ErrorPrefix => "Invite";

    protected override string LinkName => "invite link";

    protected override string RedemptionPage => "invite";

    public static Invite ForManagerOfNewTeam(Invitee invitee, string teamName, UserId invitedBy, OneTimeToken token, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);
        return new Invite(invitee, StaffRole.Manager, teamId: null, teamName, invitedBy, token, now, Lifetime);
    }

    public static Invite ForManagerOfTeam(Invitee invitee, TeamId teamId, UserId invitedBy, OneTimeToken token, DateTimeOffset now) =>
        new(invitee, StaffRole.Manager, teamId, newTeamName: null, invitedBy, token, now, Lifetime);

    public static Invite ForInterviewer(Invitee invitee, TeamId teamId, UserId invitedBy, OneTimeToken token, DateTimeOffset now) =>
        new(invitee, StaffRole.Interviewer, teamId, newTeamName: null, invitedBy, token, now, Lifetime);

    /// <summary>The bootstrap invite for the first admin, issued by the system rather than a user (HLB-39).</summary>
    public static Invite ForAdmin(Invitee invitee, OneTimeToken token, DateTimeOffset now) =>
        new(invitee, StaffRole.Admin, teamId: null, newTeamName: null, invitedBy: null, token, now, AdminLifetime);

    /// <summary>Builds the user this invite creates, in <paramref name="team"/> for a manager or an interviewer.</summary>
    public User CreateUser(TeamId? team) => Role switch
    {
        StaffRole.Admin => User.CreateAdmin(Email, DisplayName),
        StaffRole.Manager => User.CreateManager(Email, DisplayName, team ?? throw new InvalidOperationException("A manager needs a team.")),
        StaffRole.Interviewer => User.CreateInterviewer(Email, DisplayName, team ?? throw new InvalidOperationException("An interviewer needs a team.")),
        _ => throw new InvalidOperationException($"Unknown role {Role}."),
    };
}
