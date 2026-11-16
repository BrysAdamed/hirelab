using System.Text.Json.Serialization;

namespace HireLab.TaskTypes.Abstractions.Rubrics;

/// <summary>One rubric item, identified within its rubric by a kebab-case <see cref="Id"/>. Serialised with a <c>type</c> discriminator.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ScaleCriterion), ScaleCriterion.TypeName)]
[JsonDerivedType(typeof(CheckCriterion), CheckCriterion.TypeName)]
public abstract record RubricCriterion(string Id, string Title, string Description);

/// <summary>A criterion scored 0–4 and weighted in the overall score, with an optional description per level.</summary>
public sealed record ScaleCriterion(
    string Id,
    string Title,
    string Description,
    int Weight,
    IReadOnlyList<ScaleLevel>? Levels = null) : RubricCriterion(Id, Title, Description)
{
    public const string TypeName = "scale";

    public const int MinScore = 0;

    public const int MaxScore = 4;
}

/// <summary>What one score of a <see cref="ScaleCriterion"/> means.</summary>
public sealed record ScaleLevel(int Score, string Description);

/// <summary>A Yes / No criterion with an expected answer. A failed critical check is highlighted above the score.</summary>
public sealed record CheckCriterion(
    string Id,
    string Title,
    string Description,
    CheckAnswer Expected,
    bool Critical = false) : RubricCriterion(Id, Title, Description)
{
    public const string TypeName = "check";
}

/// <summary>The answer to a <see cref="CheckCriterion"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CheckAnswer>))]
public enum CheckAnswer
{
    [JsonStringEnumMemberName("yes")]
    Yes,

    [JsonStringEnumMemberName("no")]
    No,
}
