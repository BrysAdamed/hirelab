using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Positions.Contracts;

/// <summary>
/// Identifies a candidate. Other modules store only this id and resolve display data through
/// <see cref="ICandidateDirectory"/> (ADR 0023). A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).
/// </summary>
public readonly record struct CandidateId(Guid Value) : IParsable<CandidateId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static CandidateId New() => new(Guid.CreateVersion7());

    public static CandidateId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CandidateId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new CandidateId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
