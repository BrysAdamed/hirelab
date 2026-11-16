using System.Security.Cryptography;
using HireLab.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Auth.Login;

/// <summary>
/// Verifies a login password against a throwaway hash when the email is unknown or the account can't log in, so those
/// paths cost one password hash verification, like a wrong password, and response times don't reveal which emails have
/// accounts or which accounts are deactivated or locked out.
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
