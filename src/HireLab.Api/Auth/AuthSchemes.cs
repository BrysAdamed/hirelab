using HireLab.SharedKernel.Users;

namespace HireLab.Api.Auth;

internal static class AuthSchemes
{
    public const string Staff = StaffAuthentication.Scheme;
    public const string Candidate = CandidateAuthentication.Scheme;
}

internal static class AuthPolicies
{
    public const string Staff = "Staff";
    public const string Candidate = "Candidate";
}

internal static class AuthCookies
{
    public const string Staff = "__Host-hirelab-staff";
    public const string Candidate = "__Secure-hirelab-candidate";

    public static readonly IReadOnlyList<string> CandidatePaths = ["/api/candidate", "/hubs/candidate"];
}
