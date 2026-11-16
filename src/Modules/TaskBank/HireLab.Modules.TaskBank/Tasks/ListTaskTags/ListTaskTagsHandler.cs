using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Tasks.ListTaskTags;

internal sealed record ListTaskTagsQuery;

internal sealed class ListTaskTagsHandler(TaskBankDbContext dbContext, BankTaskTypes bankTaskTypes)
    : IQueryHandler<ListTaskTagsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(ListTaskTagsQuery query, CancellationToken cancellationToken)
    {
        var registeredKeys = bankTaskTypes.RegisteredKeys;
        return await dbContext.TasksInBank()
            .Where(active => registeredKeys.Contains(active.Task.TaskTypeKey))
            .SelectMany(active => active.Version.Tags)
            .Distinct()
            .OrderBy(tag => tag)
            .ToListAsync(cancellationToken);
    }
}
