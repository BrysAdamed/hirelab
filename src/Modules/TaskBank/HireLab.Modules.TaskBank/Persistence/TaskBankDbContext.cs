using HireLab.Modules.TaskBank.Contracts;
using HireLab.Modules.TaskBank.Domain;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using HireLab.TaskTypes.Abstractions.Rubrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HireLab.Modules.TaskBank.Persistence;

internal sealed class TaskBankDbContext(DbContextOptions<TaskBankDbContext> options) : DbContext(options), IModuleDbContext
{
    /// <summary>
    /// The task visibility filter (ADR 0015), the one exception to the team filter (ADR 0009): a staff user with a team
    /// sees their team's tasks and every Global task, system tasks included. The admin, candidates and code outside a
    /// request see none. A task version is visible with its task.
    /// </summary>
    public const string VisibilityFilter = "Visibility";

    public static string Schema => "taskbank";

    public DbSet<InterviewTask> Tasks => Set<InterviewTask>();

    public DbSet<TaskVersion> TaskVersions => Set<TaskVersion>();

    private TeamId? CurrentTeam => TeamQueryFilter.CurrentTeamOf(this);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSharedKernelIdConversions();
        configurationBuilder.Properties<TaskId>().HaveConversion<TaskIdConverter>();
        configurationBuilder.Properties<TaskVersionId>().HaveConversion<TaskVersionIdConverter>();
        configurationBuilder.Properties<Rubric>().HaveConversion<RubricConversion.Converter, RubricConversion.Comparer>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskBankDbContext).Assembly);

        modelBuilder.Entity<InterviewTask>().HasQueryFilter(
            VisibilityFilter,
            task => CurrentTeam != null && (task.TeamId == CurrentTeam || task.Visibility == TaskVisibility.Global));
        modelBuilder.Entity<TaskVersion>().HasQueryFilter(
            VisibilityFilter,
            version => Tasks.Any(task => task.Id == version.TaskId));
    }

    private sealed class TaskIdConverter() : ValueConverter<TaskId, Guid>(id => id.Value, value => new TaskId(value));

    private sealed class TaskVersionIdConverter() : ValueConverter<TaskVersionId, Guid>(id => id.Value, value => new TaskVersionId(value));
}
