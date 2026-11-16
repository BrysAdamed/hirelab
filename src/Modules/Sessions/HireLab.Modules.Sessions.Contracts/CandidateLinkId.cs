using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Identifies one issued candidate link of a session (ADR 0008). Regenerating the link gives it a new id, and the
/// candidate cookie carries the id of the link it was exchanged for. A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).
/// </summary>
public readonly record struct CandidateLinkId(Guid Value) : IParsable<CandidateLinkId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static CandidateLinkId New() => new(Guid.CreateVersion7());

    public static CandidateLinkId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CandidateLinkId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new CandidateLinkId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
