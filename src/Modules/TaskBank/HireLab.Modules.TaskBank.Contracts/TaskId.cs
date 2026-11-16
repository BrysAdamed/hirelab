using System.Diagnostics.CodeAnalysis;

namespace HireLab.Modules.TaskBank.Contracts;

/// <summary>Identifies a task in the task bank. A <c>Guid</c> v7 behind a strongly typed id (ADR 0004).</summary>
public readonly record struct TaskId(Guid Value) : IParsable<TaskId>
{
    /// <summary>Creates a new id from a <c>Guid</c> v7.</summary>
    public static TaskId New() => new(Guid.CreateVersion7());

    public static TaskId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TaskId result)
    {
        var parsed = Guid.TryParse(s, provider, out var value);
        result = new TaskId(value);
        return parsed;
    }

    public override string ToString() => Value.ToString();
}
