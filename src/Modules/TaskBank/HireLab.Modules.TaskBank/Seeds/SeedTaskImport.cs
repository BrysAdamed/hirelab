using HireLab.Modules.TaskBank.Domain;
using HireLab.Modules.TaskBank.Persistence;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>
/// Imports the seed packages as system tasks (ADR 0015, ADR 0030), as the TaskBank's bootstrap step in the migration
/// worker. It is idempotent by (slug, version): a stored version is never overwritten, and one whose content hash differs
/// from its re-validated package fails the import. A new version must be the task's next number. Every package is read
/// and validated before anything is saved, and everything is saved in one transaction, so a bad package imports nothing.
/// </summary>
internal sealed partial class SeedTaskImport(
    TaskBankDbContext dbContext,
    ISeedPackageSource source,
    SeedPackageValidator validator,
    IOptions<SeedImportOptions> options,
    TimeProvider timeProvider,
    ILogger<SeedTaskImport> logger)
    : ISystemJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ImportSeedTasks)
        {
            LogDisabled(SeedImportOptions.Section, nameof(SeedImportOptions.ImportSeedTasks));
            return;
        }

        var packages = SeedPackageReader.Read(source.ReadFiles());
        var slugs = packages.Select(package => package.Slug).Distinct().ToList();
        var tasks = await dbContext.Tasks
            .IgnoreQueryFilters()
            .Include(task => task.Versions)
            .Where(task => task.IsSystem && slugs.Contains(task.Slug!))
            .ToDictionaryAsync(task => task.Slug!, StringComparer.Ordinal, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var imported = 0;
        foreach (var package in packages)
        {
            var validated = validator.Validate(package);
            tasks.TryGetValue(package.Slug, out var task);

            if (task?.Versions.SingleOrDefault(version => version.Number == package.Number) is { } stored)
            {
                EnsureUnchanged(package, stored, validated);
                continue;
            }

            if (task is null)
            {
                task = InterviewTask.CreateSystem(package.Slug, validated, now);
                tasks.Add(package.Slug, task);
                dbContext.Tasks.Add(task);
            }
            else
            {
                EnsureSameTaskType(package, task);
                EnsureNextNumber(package, expected: task.Versions.Max(version => version.Number) + 1);
                task.AddSystemVersion(validated, now);
            }

            LogImported(package.Slug, package.Number);
            imported++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogCompleted(imported, packages.Count - imported);
    }

    private static void EnsureUnchanged(SeedTaskPackage package, TaskVersion stored, ValidatedTaskVersion validated)
    {
        if (stored.ContentHash != validated.ContentHash)
        {
            throw new SeedPackageException(
                $"Seed package {package.Name} differs from the imported version (content hash {stored.ContentHash}, now {validated.ContentHash}). "
                + $"Published seed versions are immutable; restore it and add v{package.Number + 1} instead.");
        }
    }

    private static void EnsureNextNumber(SeedTaskPackage package, int expected)
    {
        if (package.Number != expected)
        {
            throw new SeedPackageException(
                $"Seed package {package.Name} can't be imported: the task's next version is v{expected}.");
        }
    }

    private static void EnsureSameTaskType(SeedTaskPackage package, InterviewTask task)
    {
        if (package.TaskTypeKey != task.TaskTypeKey)
        {
            throw new SeedPackageException(
                $"Seed package {package.Name} uses the task type '{package.TaskTypeKey}', but the task is of type '{task.TaskTypeKey}'.");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed import is off ({Section}:{Setting} is false)")]
    private partial void LogDisabled(string section, string setting);

    [LoggerMessage(Level = LogLevel.Information, Message = "Importing seed task {Slug} v{Number}")]
    private partial void LogImported(string slug, int number);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed import completed: {Imported} versions imported, {Unchanged} already imported")]
    private partial void LogCompleted(int imported, int unchanged);
}
