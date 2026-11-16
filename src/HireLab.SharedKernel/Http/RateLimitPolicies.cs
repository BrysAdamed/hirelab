namespace HireLab.SharedKernel.Http;

/// <summary>Rate limiting policies the API host registers and modules apply with <c>RequireRateLimiting</c>.</summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Per client IP, a fixed window of about 10 requests a minute (ADR 0007). For endpoints that accept a secret from an
    /// anonymous caller: login, candidate link exchange, invite and reset link redemption.
    /// </summary>
    public const string Authentication = "authentication";
}
