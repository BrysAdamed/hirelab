using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.TaskTypes.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.TaskBank.Tasks;

internal sealed class ActiveTaskVersionQuery(TaskBankDbContext dbContext, ITaskTypeRegistry taskTypes) : IActiveTaskVersionQuery
{
    public async Task<TaskVersionSnapshot?> FindAsync(TaskId taskId, CancellationToken cancellationToken = default)
    {
        var active = await dbContext.TasksInBank()
            .AsNoTracking()
            .Where(active => active.Task.Id == taskId)
            .Select(active => new { active.Task.TaskTypeKey, active.Version })
            .SingleOrDefaultAsync(cancellationToken);
        if (active is null)
        {
            return null;
        }

        var version = active.Version;
        _ = taskTypes.FindForStoredVersion(active.TaskTypeKey, version.ContentSchemaVersion, version.Id)
            ?? throw new InvalidOperationException(
                $"Task {taskId} is of task type '{active.TaskTypeKey}', which is no longer registered, so its version can't be pinned.");

        return new TaskVersionSnapshot(
            taskId,
            version.Id,
            version.Number,
            active.TaskTypeKey,
            version.ContentSchemaVersion,
            version.Title,
            version.Summary,
            version.CandidateContent,
            version.StaffContent,
            version.ReferenceAnswer,
            version.InterviewerNotes,
            version.Rubric);
    }
}
