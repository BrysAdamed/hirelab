using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Tasks.ListTasks;

/// <summary>
/// The task bank's filters, each optional: the task has every one of <see cref="Tags"/>, one of <see cref="Levels"/>, the
/// task type <see cref="TaskType"/>, and <see cref="Search"/> in its title or summary.
/// </summary>
internal sealed record ListTasksQuery(IReadOnlyList<string> Tags, IReadOnlyList<TaskLevel> Levels, string? TaskType, string? Search);

/// <summary>A task in the task bank, described by its active version.</summary>
internal sealed record TaskSummaryResponse(
    Guid Id,
    int VersionNumber,
    string TaskType,
    string Title,
    string Summary,
    TaskLevel Level,
    int EstimatedMinutes,
    IReadOnlyList<string> Tags,
    BankVisibility Visibility);

internal sealed class ListTasksHandler(TaskBankDbContext dbContext, BankTaskTypes bankTaskTypes)
    : IQueryHandler<ListTasksQuery, Result<IReadOnlyList<TaskSummaryResponse>>>
{
    private const string LikeEscape = @"\";

    public async Task<Result<IReadOnlyList<TaskSummaryResponse>>> HandleAsync(ListTasksQuery query, CancellationToken cancellationToken)
    {
        var rows = await Filter(dbContext.TasksInBank().AsNoTracking(), query)
            .OrderBy(active => active.Version.Title)
            .ThenBy(active => active.Task.Id)
            .Select(active => new
            {
                active.Task.Id,
                active.Task.TaskTypeKey,
                active.Task.IsSystem,
                active.Task.Visibility,
                VersionId = active.Version.Id,
                active.Version.Number,
                active.Version.ContentSchemaVersion,
                active.Version.Title,
                active.Version.Summary,
                active.Version.Level,
                active.Version.EstimatedMinutes,
                active.Version.Tags,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => bankTaskTypes.IsShown(row.Id, row.TaskTypeKey, row.ContentSchemaVersion, row.VersionId))
            .Select(row => new TaskSummaryResponse(
                row.Id.Value,
                row.Number,
                row.TaskTypeKey,
                row.Title,
                row.Summary,
                row.Level,
                row.EstimatedMinutes,
                row.Tags,
                BankVisibilities.Of(row.IsSystem, row.Visibility)))
            .ToList();
    }

    private static IQueryable<TaskWithActiveVersion> Filter(IQueryable<TaskWithActiveVersion> tasks, ListTasksQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.TaskType))
        {
            var taskType = query.TaskType.Trim();
            tasks = tasks.Where(active => active.Task.TaskTypeKey == taskType);
        }

        if (query.Levels.Count > 0)
        {
            var levels = query.Levels.Distinct().ToList();
            tasks = tasks.Where(active => levels.Contains(active.Version.Level));
        }

        var tags = query.Tags.Select(TaskTags.Normalise).Where(tag => tag.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (tags.Count > 0)
        {
            tasks = tasks.Where(active => tags.All(tag => active.Version.Tags.Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            tasks = tasks.Where(active =>
                EF.Functions.ILike(active.Version.Title, pattern, LikeEscape) || EF.Functions.ILike(active.Version.Summary, pattern, LikeEscape));
        }

        return tasks;
    }

    private static string EscapeLike(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
