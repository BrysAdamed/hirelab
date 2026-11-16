using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Identity.Domain;

internal readonly record struct InviteId(Guid Value) : IParsable<InviteId>
{
    public static InviteId New() => new(Guid.CreateVersion7());

    public static InviteId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out InviteId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new InviteId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
