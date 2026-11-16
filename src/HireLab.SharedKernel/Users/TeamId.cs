using System.Diagnostics.CodeAnalysis;

namespace HireLab.SharedKernel.Users;

/// <summary>
/// Identifies a team (ADR 0020). Team-owned aggregates store it and are filtered by it (ADR 0009).
/// A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).
/// </summary>
public readonly record struct TeamId(Guid Value) : IParsable<TeamId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static TeamId New() => new(Guid.CreateVersion7());

    public static TeamId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TeamId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new TeamId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
