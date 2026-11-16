using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Positions.Domain;

internal readonly record struct JobPositionId(Guid Value) : IParsable<JobPositionId>
{
    public static JobPositionId New() => new(Guid.CreateVersion7());

    public static JobPositionId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out JobPositionId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new JobPositionId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
