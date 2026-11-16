using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>Identifies an interview session. A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).</summary>
public readonly record struct InterviewSessionId(Guid Value) : IParsable<InterviewSessionId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static InterviewSessionId New() => new(Guid.CreateVersion7());

    public static InterviewSessionId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out InterviewSessionId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new InterviewSessionId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
