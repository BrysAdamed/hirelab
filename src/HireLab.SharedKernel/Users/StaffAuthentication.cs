namespace HireLab.SharedKernel.Users;

/// <summary>The staff authentication scheme, shared by the API host that configures it and the Identity module that signs in with it.</summary>
public static class StaffAuthentication
{
    /// <summary>The name of the staff cookie scheme (ADR 0007).</summary>
    public const string Scheme = "Staff";
}
