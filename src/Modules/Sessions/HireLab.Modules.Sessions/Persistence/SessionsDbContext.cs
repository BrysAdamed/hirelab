using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.Modules.TaskBank.Contracts;
using HireLab.SharedKernel.Persistence;
using HireLab.TaskTypes.Abstractions.Rubrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HireLab.Modules.Sessions.Persistence;

internal sealed class SessionsDbContext(DbContextOptions<SessionsDbContext> options) : DbContext(options), IModuleDbContext
{
    public static string Schema => "sessions";

    public DbSet<InterviewSession> Sessions => Set<InterviewSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSharedKernelIdConversions();
        configurationBuilder.Properties<InterviewSessionId>().HaveConversion<InterviewSessionIdConverter>();
        configurationBuilder.Properties<SessionTaskId>().HaveConversion<SessionTaskIdConverter>();
        configurationBuilder.Properties<CandidateLinkId>().HaveConversion<CandidateLinkIdConverter>();
        configurationBuilder.Properties<CandidateId>().HaveConversion<CandidateIdConverter>();
        configurationBuilder.Properties<TaskId>().HaveConversion<TaskIdConverter>();
        configurationBuilder.Properties<TaskVersionId>().HaveConversion<TaskVersionIdConverter>();
        configurationBuilder.Properties<Rubric>().HaveConversion<RubricConversion.Converter, RubricConversion.Comparer>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SessionsDbContext).Assembly);

        modelBuilder.Entity<SessionTask>().HasQueryFilter(
            TeamQueryFilter.Name,
            task => Sessions.Any(session => session.Id == task.SessionId));
        modelBuilder.Entity<Response>().HasQueryFilter(
            TeamQueryFilter.Name,
            response => Sessions.Any(session => session.Id == response.SessionId));
    }

    private sealed class InterviewSessionIdConverter() : ValueConverter<InterviewSessionId, Guid>(id => id.Value, value => new InterviewSessionId(value));

    private sealed class SessionTaskIdConverter() : ValueConverter<SessionTaskId, Guid>(id => id.Value, value => new SessionTaskId(value));

    private sealed class CandidateLinkIdConverter() : ValueConverter<CandidateLinkId, Guid>(id => id.Value, value => new CandidateLinkId(value));

    private sealed class CandidateIdConverter() : ValueConverter<CandidateId, Guid>(id => id.Value, value => new CandidateId(value));

    private sealed class TaskIdConverter() : ValueConverter<TaskId, Guid>(id => id.Value, value => new TaskId(value));

    private sealed class TaskVersionIdConverter() : ValueConverter<TaskVersionId, Guid>(id => id.Value, value => new TaskVersionId(value));
}
