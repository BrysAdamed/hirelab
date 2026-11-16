using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HireLab.Testing.Staff;

/// <summary>Counts the password hash verifications the API performs, for tests of timing-sensitive paths such as login.</summary>
public sealed class PasswordHashProbe
{
    private int _verifications;

    /// <summary>The verifications since the last <see cref="Reset"/>.</summary>
    public int Verifications => Volatile.Read(ref _verifications);

    /// <summary>Starts counting from zero.</summary>
    public void Reset() => Interlocked.Exchange(ref _verifications, 0);

    internal void Verified() => Interlocked.Increment(ref _verifications);
}

internal sealed class ProbingPasswordHasher(PasswordHashProbe probe, IOptions<PasswordHasherOptions> options) : IPasswordHasher<User>
{
    private readonly PasswordHasher<User> _inner = new(options);

    public string HashPassword(User user, string password) => _inner.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        probe.Verified();
        return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}
