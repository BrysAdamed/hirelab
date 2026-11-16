using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Identity.Domain;

internal readonly record struct ResetLinkId(Guid Value) : IParsable<ResetLinkId>
{
    public static ResetLinkId New() => new(Guid.CreateVersion7());

    public static ResetLinkId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ResetLinkId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new ResetLinkId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
