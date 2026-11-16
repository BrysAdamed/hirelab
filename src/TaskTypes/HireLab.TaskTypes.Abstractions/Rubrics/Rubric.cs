namespace HireLab.TaskTypes.Abstractions.Rubrics;

/// <summary>The scoring criteria of a task version (ADR 0013). Staff only: it never reaches the candidate API.</summary>
public sealed record Rubric(IReadOnlyList<RubricCriterion> Criteria);
