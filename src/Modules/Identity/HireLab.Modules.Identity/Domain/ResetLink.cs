using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Domain;

/// <summary>
/// A reset link (ADR 0007, ADR 0021): redeeming it sets a new password for an existing user, clears their lockout and
/// changes their security stamp. There is no self-service reset; an admin or a manager issues the link.
/// </summary>
internal sealed class ResetLink : OneTimeLink
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private ResetLink()
    {
    }

    private ResetLink(UserId userId, UserId? issuedBy, OneTimeToken token, DateTimeOffset now)
        : base(token, now, Lifetime)
    {
        Id = ResetLinkId.New();
        UserId = userId;
        IssuedBy = issuedBy;
    }

    public ResetLinkId Id { get; private set; }

    /// <summary>The user whose password the link resets.</summary>
    public UserId UserId { get; private set; }

    /// <summary>The user who issued the link; <c>null</c> when the system issued it.</summary>
    public UserId? IssuedBy { get; private set; }

    protected override string ErrorPrefix => "ResetLink";

    protected override string LinkName => "reset link";

    public static ResetLink For(UserId userId, UserId? issuedBy, OneTimeToken token, DateTimeOffset now) =>
        new(userId, issuedBy, token, now);
}
