using System.Text.Json;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Tasks.GetTask;

internal sealed record GetTaskQuery(TaskId TaskId);

/// <summary>A task in the task bank with everything staff see of its active version, both content parts included.</summary>
internal sealed record TaskResponse(
    Guid Id,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary,
    TaskLevel Level,
    int EstimatedMinutes,
    IReadOnlyList<string> Tags,
    BankVisibility Visibility,
    string ReferenceAnswer,
    string? InterviewerNotes,
    Rubric Rubric,
    JsonElement CandidateContent,
    JsonElement StaffContent);

internal sealed class GetTaskHandler(TaskBankDbContext dbContext, BankTaskTypes bankTaskTypes)
    : IQueryHandler<GetTaskQuery, Result<TaskResponse>>
{
    public async Task<Result<TaskResponse>> HandleAsync(GetTaskQuery query, CancellationToken cancellationToken)
    {
        var active = await dbContext.TasksInBank()
            .AsNoTracking()
            .Where(active => active.Task.Id == query.TaskId)
            .Select(active => new { active.Task.TaskTypeKey, active.Task.IsSystem, active.Task.Visibility, active.Version })
            .SingleOrDefaultAsync(cancellationToken);
        if (active is null)
        {
            return TaskErrors.NotFound;
        }

        var version = active.Version;
        if (!bankTaskTypes.IsShown(query.TaskId, active.TaskTypeKey, version.ContentSchemaVersion, version.Id))
        {
            return TaskErrors.NotFound;
        }

        return new TaskResponse(
            query.TaskId.Value,
            version.Number,
            active.TaskTypeKey,
            version.Title,
            version.Summary,
            version.Level,
            version.EstimatedMinutes,
            version.Tags,
            BankVisibilities.Of(active.IsSystem, active.Visibility),
            version.ReferenceAnswer,
            version.InterviewerNotes,
            version.Rubric,
            version.CandidateContent,
            version.StaffContent);
    }
}
