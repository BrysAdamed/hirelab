using System.Security.Cryptography;
using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Auth.Login;

/// <summary>
/// Verifies a login password against a throwaway hash when the email is unknown, so that path costs one password hash
/// verification, like a wrong password, and response times don't reveal which emails have accounts.
/// </summary>
internal sealed class UnknownUserPasswordCheck(IOptions<PasswordHasherOptions> options)
{
    private static readonly User _nobody = User.CreateAdmin("nobody@example.invalid", "Nobody");

    private readonly Lazy<string> _hash = new(() =>
        new PasswordHasher<User>(options).HashPassword(_nobody, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));

    public void Verify(IPasswordHasher<User> hasher, string password)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.VerifyHashedPassword(_nobody, _hash.Value, password);
    }
}
