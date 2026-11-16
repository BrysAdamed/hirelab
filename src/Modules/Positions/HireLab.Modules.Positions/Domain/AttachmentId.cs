using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Positions.Domain;

internal readonly record struct AttachmentId(Guid Value) : IParsable<AttachmentId>
{
    public static AttachmentId New() => new(Guid.CreateVersion7());

    public static AttachmentId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out AttachmentId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new AttachmentId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
