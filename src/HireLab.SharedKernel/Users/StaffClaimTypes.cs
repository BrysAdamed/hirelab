using System.Security.Claims;

namespace HireLab.SharedKernel.Users;

/// <summary>
/// The claims the staff cookie carries. The Identity module issues them and <see cref="ICurrentUser"/> reads them.
/// </summary>
public static class StaffClaimTypes
{
    /// <summary>The user's <see cref="Users.UserId"/>.</summary>
    public const string UserId = ClaimTypes.NameIdentifier;

    /// <summary>The user's <see cref="StaffRole"/>, by name, so role-based authorisation works on it.</summary>
    public const string Role = ClaimTypes.Role;

    /// <summary>The user's <see cref="Users.TeamId"/>. Absent for the admin.</summary>
    public const string TeamId = "hirelab:team_id";
}
