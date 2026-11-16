using System.Diagnostics.CodeAnalysis;

namespace HireLab.SharedKernel.Users;

/// <summary>Identifies a staff user. A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).</summary>
public readonly record struct UserId(Guid Value) : IParsable<UserId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static UserId New() => new(Guid.CreateVersion7());

    public static UserId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UserId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new UserId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
