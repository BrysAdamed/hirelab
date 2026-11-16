namespace HireLab.SharedKernel.Users;

/// <summary>
/// The staff user of the current request, read from the staff cookie's claims. Handlers use it for finer rules and
/// module DbContexts use <see cref="TeamId"/> for the team query filter (ADR 0009).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Whether the request comes from a signed-in staff user. Candidates and anonymous requests are not.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The user's id. Throws when <see cref="IsAuthenticated"/> is <c>false</c>.</summary>
    UserId Id { get; }

    /// <summary>The user's role. Throws when <see cref="IsAuthenticated"/> is <c>false</c>.</summary>
    StaffRole Role { get; }

    /// <summary>The user's team; <c>null</c> for the admin and when <see cref="IsAuthenticated"/> is <c>false</c>.</summary>
    TeamId? TeamId { get; }
}
