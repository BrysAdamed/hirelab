using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.TaskBank.Contracts;

/// <summary>Identifies a task version. A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).</summary>
public readonly record struct TaskVersionId(Guid Value) : IParsable<TaskVersionId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static TaskVersionId New() => new(Guid.CreateVersion7());

    public static TaskVersionId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TaskVersionId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new TaskVersionId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
