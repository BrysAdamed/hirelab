namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// A candidate's contact details: a full name, an optional email and phone, and up to <see cref="MaxLinks"/> profile
/// links. A blank optional value is stored as <c>null</c>.
/// </summary>
internal sealed record CandidateDetails(string FullName, string? Email, string? Phone, IReadOnlyList<string> Links)
{
    public const int FullNameMaxLength = 200;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 50;
    public const int LinkMaxLength = 500;
    public const int MaxLinks = 3;

    public CandidateDetails Normalised() => new(
        FullName.Trim(),
        Optional(Email),
        Optional(Phone),
        [.. Links.Select(link => link.Trim())]);

    public static bool IsProfileLink(string? link) =>
        Uri.TryCreate(link?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrEmpty(uri.Host);

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
