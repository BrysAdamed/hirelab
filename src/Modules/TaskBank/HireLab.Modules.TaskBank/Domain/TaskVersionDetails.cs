using System.Text.Json;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// What an author writes for a task version, as the editor or a seed package supplies it. It becomes a task version
/// only through <see cref="TaskVersionValidator"/>, which checks and normalises it.
/// </summary>
internal sealed record TaskVersionDetails(
    string Title,
    string Summary,
    TaskLevel? Level,
    int EstimatedMinutes,
    IReadOnlyList<string>? Tags,
    string ReferenceAnswer,
    string? InterviewerNotes,
    Rubric? Rubric,
    JsonElement CandidateContent,
    JsonElement StaffContent)
{
    public const int TitleMaxLength = 120;
    public const int SummaryMaxLength = 1000;
    public const int MinEstimatedMinutes = 1;
    public const int MaxEstimatedMinutes = 180;
    public const int ContentMaxBytes = 1024 * 1024;
}
