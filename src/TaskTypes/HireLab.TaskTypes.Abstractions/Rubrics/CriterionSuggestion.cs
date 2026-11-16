using System.Text.Json.Serialization;

namespace HireLab.TaskTypes.Abstractions.Rubrics;

/// <summary>
/// A task type's suggested value for one rubric criterion, with the <see cref="Reason"/> shown to the interviewer,
/// who confirms or overrides it. Serialised with a <c>type</c> discriminator matching the criterion's.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ScaleSuggestion), ScaleCriterion.TypeName)]
[JsonDerivedType(typeof(CheckSuggestion), CheckCriterion.TypeName)]
public abstract record CriterionSuggestion(string CriterionId, string Reason);

/// <summary>A suggested score for a <see cref="ScaleCriterion"/>.</summary>
public sealed record ScaleSuggestion : CriterionSuggestion
{
    public ScaleSuggestion(string criterionId, int score, string reason)
        : base(criterionId, reason)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(score, ScaleCriterion.MinScore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(score, ScaleCriterion.MaxScore);
        Score = score;
    }

    public int Score { get; }
}

/// <summary>A suggested answer for a <see cref="CheckCriterion"/>.</summary>
public sealed record CheckSuggestion(string CriterionId, CheckAnswer Answer, string Reason) : CriterionSuggestion(CriterionId, Reason);
