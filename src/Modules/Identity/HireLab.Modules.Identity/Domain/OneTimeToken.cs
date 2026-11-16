using System.Security.Cryptography;
using System.Text;

namespace HireLab.Modules.Identity.Domain;

/// <summary>
/// The secret of a one-time link: 256 random bits, base64url encoded. Only its SHA-256 <see cref="Hash"/> is stored;
/// the <see cref="Value"/> goes into the link once and is never persisted or logged.
/// </summary>
internal sealed record OneTimeToken(string Value, string Hash)
{
    public const int MaxLength = 128;

    private const int SizeInBytes = 32;

    public static OneTimeToken New()
    {
        var value = Base64Url(RandomNumberGenerator.GetBytes(SizeInBytes));
        return new OneTimeToken(value, HashOf(value));
    }

    public static string HashOf(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    public override string ToString() => nameof(OneTimeToken);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
