using Microsoft.AspNetCore.Authorization;

namespace HireLab.SharedKernel.Users;

/// <summary>
/// The staff role policies (ADR 0009). Apply one at the route-group or endpoint level, on top of the staff group's
/// authentication, for example <c>group.RequireAuthorization(StaffPolicies.Manager)</c>. A signed-in user in another
/// role gets 403 ProblemDetails.
/// </summary>
public static class StaffPolicies
{
    /// <summary>Only the admin: user and team management.</summary>
    public const string Admin = nameof(Admin);

    /// <summary>Only managers: closing positions, final decisions, managing their interviewers.</summary>
    public const string Manager = nameof(Manager);

    /// <summary>Managers and interviewers: the team's work, never the admin.</summary>
    public const string ManagerOrInterviewer = nameof(ManagerOrInterviewer);

    /// <summary>Registers the staff role policies on the staff authentication scheme.</summary>
    public static AuthorizationBuilder AddStaffPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddPolicy(Admin, policy => RequireRoles(policy, StaffRole.Admin))
            .AddPolicy(Manager, policy => RequireRoles(policy, StaffRole.Manager))
            .AddPolicy(ManagerOrInterviewer, policy => RequireRoles(policy, StaffRole.Manager, StaffRole.Interviewer));
    }

    private static void RequireRoles(AuthorizationPolicyBuilder policy, params StaffRole[] roles) =>
        policy
            .AddAuthenticationSchemes(StaffAuthentication.Scheme)
            .RequireAuthenticatedUser()
            .RequireRole(roles.Select(role => role.ToString()));
}
