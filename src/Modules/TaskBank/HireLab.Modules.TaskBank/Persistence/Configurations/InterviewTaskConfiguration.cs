using HireLab.Modules.TaskBank.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireLab.Modules.TaskBank.Persistence.Configurations;

internal sealed class InterviewTaskConfiguration : IEntityTypeConfiguration<InterviewTask>
{
    public const string SlugIndex = "ix_tasks_slug";

    public void Configure(EntityTypeBuilder<InterviewTask> builder)
    {
        builder.ToTable("tasks", table => table.HasCheckConstraint(
            "ck_tasks_owner",
            "(is_system AND team_id IS NULL AND slug IS NOT NULL AND visibility = 'Global') "
            + "OR (NOT is_system AND team_id IS NOT NULL AND slug IS NULL)"));

        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.Visibility).HasConversion<string>().HasMaxLength(20);
        builder.Property(task => task.Slug).HasMaxLength(100);
        builder.Property(task => task.TaskTypeKey).HasMaxLength(64);
        builder.Property(task => task.Version).IsRowVersion();
        builder.Ignore(task => task.ActiveVersion);
        builder.Ignore(task => task.Draft);

        builder.HasMany(task => task.Versions)
            .WithOne()
            .HasForeignKey(version => version.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(task => task.Slug).IsUnique().HasDatabaseName(SlugIndex);
        builder.HasIndex(task => task.TeamId).HasDatabaseName("ix_tasks_team_id");
    }
}
