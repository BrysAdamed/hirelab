using System.Text.Json;
using HireLab.TaskTypes.Abstractions;
using HireLab.TaskTypes.Abstractions.Rubrics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HireLab.Modules.TaskBank.Persistence;

/// <summary>Stores a <see cref="Rubric"/> as JSON written with <see cref="TaskTypeJson.Options"/> (ADR 0028).</summary>
internal static class RubricConversion
{
    private static string Serialise(Rubric rubric) => JsonSerializer.Serialize(rubric, TaskTypeJson.Options);

    private static Rubric Deserialise(string json) =>
        JsonSerializer.Deserialize<Rubric>(json, TaskTypeJson.Options)
        ?? throw new InvalidOperationException("A stored rubric is null.");

    internal sealed class Converter() : ValueConverter<Rubric, string>(rubric => Serialise(rubric), json => Deserialise(json));

    internal sealed class Comparer() : ValueComparer<Rubric>(
        (left, right) => left == null ? right == null : right != null && Serialise(left) == Serialise(right),
        rubric => Serialise(rubric).GetHashCode(StringComparison.Ordinal),
        rubric => Deserialise(Serialise(rubric)));
}
