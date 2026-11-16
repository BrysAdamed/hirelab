using System.Text.Json;
using HireLab.Modules.TaskBank.Domain;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>A seed package's <c>task.json</c> after its file references are inlined (ADR 0030).</summary>
internal sealed record SeedTaskDocument(
    string TaskType,
    string Title,
    string Summary,
    TaskLevel Level,
    int EstimatedMinutes,
    string ReferenceAnswer,
    Rubric Rubric,
    JsonElement CandidateContent,
    JsonElement StaffContent,
    IReadOnlyList<string>? Tags = null,
    string? InterviewerNotes = null)
{
    public TaskVersionDetails ToDetails() => new(
        Title,
        Summary,
        Level,
        EstimatedMinutes,
        Tags ?? [],
        ReferenceAnswer,
        InterviewerNotes,
        Rubric,
        CandidateContent,
        StaffContent);
}
