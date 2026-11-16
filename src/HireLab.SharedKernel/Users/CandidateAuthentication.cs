namespace HireLab.SharedKernel.Users;

/// <summary>The candidate authentication scheme, shared by the API host that configures it and the Sessions module that signs in with it.</summary>
public static class CandidateAuthentication
{
    /// <summary>The name of the candidate cookie scheme (ADR 0008).</summary>
    public const string Scheme = "Candidate";
}
