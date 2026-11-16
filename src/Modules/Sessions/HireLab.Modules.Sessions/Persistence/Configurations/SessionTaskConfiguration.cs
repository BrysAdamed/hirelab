using HireLab.Modules.Sessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.Sessions.Persistence.Configurations;

internal sealed class SessionTaskConfiguration : IEntityTypeConfiguration<SessionTask>
{
    public void Configure(EntityTypeBuilder<SessionTask> builder)
    {
        builder.ToTable("session_tasks");

        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(task => task.TaskTypeKey).HasMaxLength(100);
        builder.Property(task => task.Title).HasMaxLength(200);
        builder.Property(task => task.Summary).HasMaxLength(2000);
        builder.Property(task => task.CandidateContent).HasColumnType("jsonb");
        builder.Property(task => task.StaffContent).HasColumnType("jsonb");
        builder.Property(task => task.Rubric).HasColumnType("jsonb");
        builder.Ignore(task => task.IsPending);

        builder.HasIndex(task => new { task.SessionId, task.Position }).HasDatabaseName("ix_session_tasks_session_id_position");
        builder.HasIndex(task => task.TaskId).HasDatabaseName("ix_session_tasks_task_id");
    }
}
