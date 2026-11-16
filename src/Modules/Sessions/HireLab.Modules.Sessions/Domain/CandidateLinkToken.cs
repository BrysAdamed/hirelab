using System.Security.Cryptography;
using System.Text;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// The secret of a candidate link (ADR 0008): 256 random bits, base64url encoded. Only its SHA-256 <see cref="Hash"/>
/// is stored; the <see cref="Value"/> goes into the link once and is never persisted or logged.
/// </summary>
internal sealed record CandidateLinkToken(string Value, string Hash)
{
    public const int MaxLength = 128;

    private const int SizeInBytes = 32;

    public static CandidateLinkToken New()
    {
        var value = Base64Url(RandomNumberGenerator.GetBytes(SizeInBytes));
        return new CandidateLinkToken(value, HashOf(value));
    }

    public static string HashOf(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    /// <summary>The path of the candidate app page the link opens. The candidate app sends the token on and removes it from the address bar.</summary>
    public string LinkPath => $"/s/{Value}";

    public override string ToString() => nameof(CandidateLinkToken);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
