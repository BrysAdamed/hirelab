using System.Text.Json;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.Modules.TaskBank.Contracts;

/// <summary>
/// Everything a session task copies from the task version it pins (ADR 0013, ADR 0028). Published task versions are
/// immutable, so the snapshot of a version id never changes. Staff only: the staff content, reference answer,
/// interviewer notes and rubric must never reach the candidate API.
/// </summary>
/// <param name="TaskId">The task.</param>
/// <param name="VersionId">The task version, the one to pin.</param>
/// <param name="Number">The version's number, shown as <c>vN</c>.</param>
/// <param name="TaskTypeKey">The task type key, which picks the candidate and review UI.</param>
/// <param name="ContentSchemaVersion">The content schema version the content was validated with.</param>
/// <param name="Title">The version's title.</param>
/// <param name="Summary">The version's summary.</param>
/// <param name="CandidateContent">The content part the candidate sees.</param>
/// <param name="StaffContent">The staff-only content part.</param>
/// <param name="ReferenceAnswer">Markdown describing a strong answer.</param>
/// <param name="InterviewerNotes">Markdown hints and follow-up questions, if any.</param>
/// <param name="Rubric">The scoring criteria.</param>
public sealed record TaskVersionSnapshot(
    TaskId TaskId,
    TaskVersionId VersionId,
    int Number,
    string TaskTypeKey,
    int ContentSchemaVersion,
    string Title,
    string Summary,
    JsonElement CandidateContent,
    JsonElement StaffContent,
    string ReferenceAnswer,
    string? InterviewerNotes,
    Rubric Rubric);
