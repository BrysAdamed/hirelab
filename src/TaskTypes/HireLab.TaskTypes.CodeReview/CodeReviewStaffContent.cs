using System.Text.Json.Serialization;

namespace HireLab.TaskTypes.CodeReview;

/// <summary>The staff-only part: the issues the author planted in the pull request.</summary>
public sealed record CodeReviewStaffContent(IReadOnlyList<ExpectedFinding> ExpectedFindings);

/// <summary>
/// An issue the author planted, identified within the task by a kebab-case <see cref="Id"/>. It points at
/// <see cref="StartLine"/>–<see cref="EndLine"/> (1-based, inclusive) of one <see cref="Side"/> of the changed file
/// whose path is <see cref="File"/>.
/// </summary>
public sealed record ExpectedFinding(
    string Id,
    string File,
    DiffSide Side,
    int StartLine,
    int EndLine,
    string Title,
    string Description,
    FindingSeverity Severity);

/// <summary>How serious an expected finding is. Serialised by name: <c>Critical</c>, <c>Major</c>, <c>Minor</c>, <c>Nit</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FindingSeverity>))]
public enum FindingSeverity
{
    Critical,
    Major,
    Minor,
    Nit,
}
