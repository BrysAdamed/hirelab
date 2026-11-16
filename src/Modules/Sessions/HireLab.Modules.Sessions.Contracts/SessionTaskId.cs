using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>Identifies a session task (one task in a session's queue). A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).</summary>
public readonly record struct SessionTaskId(Guid Value) : IParsable<SessionTaskId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static SessionTaskId New() => new(Guid.CreateVersion7());

    public static SessionTaskId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SessionTaskId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new SessionTaskId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
