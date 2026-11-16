using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Tasks.ListTaskTypes;

internal sealed record ListTaskTypesQuery;

internal sealed class ListTaskTypesHandler(TaskBankDbContext dbContext, BankTaskTypes bankTaskTypes)
    : IQueryHandler<ListTaskTypesQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> HandleAsync(ListTaskTypesQuery query, CancellationToken cancellationToken)
    {
        var registeredKeys = bankTaskTypes.RegisteredKeys;
        return await dbContext.TasksInBank()
            .Where(active => registeredKeys.Contains(active.Task.TaskTypeKey))
            .Select(active => active.Task.TaskTypeKey)
            .Distinct()
            .OrderBy(taskType => taskType)
            .ToListAsync(cancellationToken);
    }
}
